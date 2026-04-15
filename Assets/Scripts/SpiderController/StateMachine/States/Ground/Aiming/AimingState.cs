using DG.Tweening;
using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CutScene;
using Infastructure.Services.Magnet;
using Infastructure.Services.Pause;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Infastructure.StaticData.StaticDataService;
using SpiderController.TriggerChecker;
using UnityEngine;

namespace SpiderController.StateMachine.States.Ground.Aiming
{
    public class AimingState : GroundedState
    {
        private readonly Vector3 _defaultPosition;

        protected AimingState(
            ISpiderStateMachine stateMachine,
            SpiderServiceContext serviceContext,
            SpiderStateContext stateContext,
            EnergySystem energySystem) : base(stateMachine, serviceContext, stateContext, energySystem)
        {
        }

        private Transform RotationPlaneTransform => StateContext.RotationPlaneTransform;

        private Tween _localMoveTween;


        public override void Enter()
        {
            base.Enter();

            Data.AimingStateChanged += OnAimingStateChanged;

            if (Data.IsInAimingState == false)
            {
                MagnetFreezingService.FreezeForAiming();
                Data.IsInAimingState = true;

                _localMoveTween?.Kill();
                _localMoveTween = RotationPlaneTransform.DOLocalMove(Vector3.zero, 0.5f);
            }
        }

        public override void Exit()
        {
            base.Exit();

            Data.AimingStateChanged -= OnAimingStateChanged;
        }

        public override void Update()
        {
            base.Update();

            if (InputService.CenterMouseUp)
            {
                ThrowAllObjects();

                if (IsInputZero())
                    StateMachine.SwitchState<IdlingState>();
                else
                    StateMachine.SwitchState<RunningState>();
            }
        }

        private void OnAimingStateChanged()
        {
            if (Data.IsInAimingState == false)
                ThrowAllObjects();
        }

        private void ThrowAllObjects()
        {
            Data.IsInAimingState = false;
            MagnetFreezingService.UnfreezeForAiming();

            Vector3 targetPosition = RotationPlaneTransform.localPosition;
            targetPosition.y = _defaultPosition.y;

            PlatformObjectsService.ThrowAll();

            _localMoveTween?.Kill();
            _localMoveTween = RotationPlaneTransform.DOLocalMove(targetPosition, 0.05f);
        }
    }
}