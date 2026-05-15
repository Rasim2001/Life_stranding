using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infastructure.Services.PickupRewindRegistry;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using PickupObjects.PickUpOnPlatform;
using PickupObjects.Rewind;
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
        private IPickupRewindRegistryService PickupRewindRegistry => ServiceContext.PickupRewindRegistryService;
        private IPlatformObjectsService PlatformObjectsService => ServiceContext.PlatformObjectsService;

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
            InputService.LockInput();
            PickupRewindRegistry.PauseRecording();

            foreach (IPickupRewindable obj in PickupRewindRegistry.All)
                obj.FreezeForRewind();

            BodyOrientation.Freeze();
            Data.CanRecordFootprints = false;

            Rigidbody.isKinematic = true;
            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;

            _cts = new CancellationTokenSource();
            RunRewindAsync(_cts.Token).Forget();
        }

        public override void Exit()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            Rigidbody.isKinematic = false;
            Data.CanRecordFootprints = true;

            PickupRewindRegistry.ResumeRecording();
            BodyOrientation.Unfreeze();

            _recorder.Clear();
            PickupRewindRegistry.ClearAll();

            InputService.UnlockInput();
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


        private async UniTask RunRewindAsync(CancellationToken token)
        {
            if (!_recorder.HasSnapshots)
            {
                StateMachine.SwitchState<IdlingState>();
                return;
            }

            SpiderSnapshot[] spiderSnaps = _recorder.GetSnapshotsReversed();

            IPickupRewindable[] pickupObjects = PickupRewindRegistry.All.ToArray();
            PickupObjectSnapshot[][] pickupSnaps = new PickupObjectSnapshot[pickupObjects.Length][];
            for (int j = 0; j < pickupObjects.Length; j++)
                pickupSnaps[j] = PickupRewindRegistry.GetSnapshotsReversed(pickupObjects[j]);

            try
            {
                await UniTask.WhenAll(
                    PlayRewindAsync(spiderSnaps, token),
                    PlayRewindPickupObjectsAsync(pickupObjects, pickupSnaps, token));
            }
            catch (OperationCanceledException)
            {
                return;
            }

            StateMachine.SwitchState<IdlingState>();
        }

        private async UniTask PlayRewindAsync(SpiderSnapshot[] snapshots, CancellationToken token)
        {
            for (int i = 0; i < snapshots.Length - 1; i++)
            {
                SpiderSnapshot from = snapshots[i];
                SpiderSnapshot to = snapshots[i + 1];

                float segmentDuration = (from.Time - to.Time) / PlaybackSpeed;
                float segmentElapsed = 0f;

                ApplyPlatformId(to);

                while (segmentElapsed < segmentDuration)
                {
                    float t = segmentElapsed / segmentDuration;

                    ApplySpiderTransform(from, to, t);
                    ApplyLegPositions(from, to, t);
                    ApplyBodyRotations(from, to, t);
                    ApplyPlaneRotations(from, to, t);

                    await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
                    segmentElapsed += Time.fixedDeltaTime;
                }
            }

            ApplySnapshot(snapshots[^1]);
        }

        private async UniTask PlayRewindPickupObjectsAsync(
            IPickupRewindable[] pickupObjects,
            PickupObjectSnapshot[][] pickupSnaps,
            CancellationToken token)
        {
            if (pickupObjects.Length == 0)
                return;

            int segmentCount = pickupSnaps[0].Length - 1;

            for (int i = 0; i < segmentCount; i++)
            {
                float segmentDuration = (pickupSnaps[0][i].Time - pickupSnaps[0][i + 1].Time) / PlaybackSpeed;
                float segmentElapsed = 0f;

                while (segmentElapsed < segmentDuration)
                {
                    float t = segmentElapsed / segmentDuration;

                    for (int j = 0; j < pickupObjects.Length; j++)
                        pickupObjects[j].ApplyLerp(pickupSnaps[j][i], pickupSnaps[j][i + 1], t);

                    await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
                    segmentElapsed += Time.fixedDeltaTime;
                }
            }

            for (int j = 0; j < pickupObjects.Length; j++)
            {
                PickupObjectSnapshot snapshot = pickupSnaps[j][^1];

                if (snapshot.IsOnPlatform)
                    PlatformObjectsService.AddAfterRewind(pickupObjects[j] as PickupObjectBase);

                pickupObjects[j].ApplyFinalSnapshot(snapshot);
            }
        }


        private void ApplySpiderTransform(SpiderSnapshot from, SpiderSnapshot to, float t)
        {
            Transform.position = Vector3.Lerp(from.Position, to.Position, t);
            Transform.rotation = Quaternion.Slerp(from.Rotation, to.Rotation, t);
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