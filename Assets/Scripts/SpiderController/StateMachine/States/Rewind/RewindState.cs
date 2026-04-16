using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SpiderController.Platform;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Ground;
using UnityEngine;

namespace SpiderController.StateMachine.States.Rewind
{
    public class RewindState : MovementState
    {
        private const float PlaybackSpeed = 2f;

        private readonly SpiderRewindRecorder _recorder;
        private readonly PlatformSelector _platformSelector;

        private BodyOrientation BodyOrientation => StateContext.BodyOrientation;

        private CancellationTokenSource _cts;

        protected RewindState(
            ISpiderStateMachine stateMachine,
            SpiderServiceContext serviceContext,
            SpiderStateContext stateContext,
            EnergySystem energySystem,
            SpiderRewindRecorder recorder,
            PlatformSelector platformSelector)
            : base(stateMachine, serviceContext, stateContext, energySystem)
        {
            _recorder = recorder;
            _platformSelector = platformSelector;
        }

        public override void Enter()
        {
            LockInput();

            BodyOrientation.Freeze();
            Data.CanRecordFootprints = false;

            Rigidbody.isKinematic = true;
            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;

            _cts = new CancellationTokenSource();
            PlayRewindAsync(_cts.Token).Forget();
        }

        public override void Exit()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            Rigidbody.isKinematic = false;
            Data.CanRecordFootprints = true;
            BodyOrientation.Unfreeze();

            UnlockInput();

            _recorder.Clear();
        }

        public override void HandleInput()
        {
        }

        public override void Update()
        {
        }

        public override void FixedUpdate()
        {
        }

        private void LockInput() =>
            ServiceContext.CutSceneService.IsActive = true;

        private void UnlockInput() =>
            ServiceContext.CutSceneService.IsActive = false;

        private async UniTaskVoid PlayRewindAsync(CancellationToken token)
        {
            if (!_recorder.HasSnapshots)
            {
                StateMachine.SwitchState<IdlingState>();
                return;
            }

            SpiderSnapshot[] snapshots = _recorder.GetSnapshotsReversed();

            try
            {
                for (int i = 0; i < snapshots.Length - 1; i++)
                {
                    SpiderSnapshot from = snapshots[i];
                    SpiderSnapshot to = snapshots[i + 1];

                    float segmentDuration = (from.Time - to.Time) / PlaybackSpeed;

                    float segmentElapsed = 0f;

                    while (segmentElapsed < segmentDuration)
                    {
                        float t = segmentElapsed / segmentDuration;

                        Transform.position = Vector3.Lerp(from.Position, to.Position, t);
                        Transform.rotation = Quaternion.Slerp(from.Rotation, to.Rotation, t);

                        ApplyLegPositions(from, to, t);
                        ApplyBodyRotations(from, to, t);
                        ApplyPlaneRotations(from, to, t);
                        ApplyPlatformId(to);

                        await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);

                        segmentElapsed += Time.fixedDeltaTime;
                    }
                }

                SpiderSnapshot target = snapshots[^1];
                ApplySnapshot(target);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            StateMachine.SwitchState<IdlingState>();
        }

        private void ApplyPlatformId(SpiderSnapshot to) =>
            _platformSelector.SetPlatformFromRewind(to.PlatformIndex);

        private void ApplyPlaneRotations(SpiderSnapshot from, SpiderSnapshot to, float t)
        {
            Quaternion planeRot = Quaternion.Slerp(from.PlaneRotation, to.PlaneRotation, t);

            StateContext.RotationPlaneTransform.localRotation = planeRot;
        }

        private void ApplyLegPositions(SpiderSnapshot from, SpiderSnapshot to, float t)
        {
            for (int i = 0; i < Legs.Length; i++)
            {
                if (i >= from.LegPositions.Length || i >= to.LegPositions.Length)
                    break;

                Vector3 legTarget = Vector3.Lerp(from.LegPositions[i], to.LegPositions[i], t);
                Legs[i].Leg.SetPositionImmediate(legTarget);
            }
        }

        private void ApplyBodyRotations(SpiderSnapshot from, SpiderSnapshot to, float t)
        {
            Quaternion legsRot = Quaternion.Slerp(from.LegsRootRotation, to.LegsRootRotation, t);
            Quaternion raycastRot = Quaternion.Slerp(from.RaycastRigRotation, to.RaycastRigRotation, t);
            Quaternion headRoot = Quaternion.Slerp(from.HeadRootRotation, to.HeadRootRotation, t);

            StateContext.BodyOrientation.SetBonesRotation(legsRot, raycastRot, headRoot);
        }

        private void ApplySnapshot(SpiderSnapshot snapshot)
        {
            Transform.position = snapshot.Position;
            Transform.rotation = snapshot.Rotation;

            StateContext.Data.CurrentEnergyFillAmount = snapshot.CurrentEnergy;

            float hpDiff = snapshot.CurrentHp - StateContext.SpiderUI.SpiderHealth.CurrentHP;
            if (hpDiff > 0)
                StateContext.SpiderUI.SpiderHealth.Heal(hpDiff);
        }
    }
}