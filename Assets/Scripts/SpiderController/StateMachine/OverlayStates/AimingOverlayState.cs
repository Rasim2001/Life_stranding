using DG.Tweening;
using Infastructure.Services.Magnet;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using SpiderController.Trajectory;
using UnityEngine;

namespace SpiderController.StateMachine.OverlayStates
{
    public class AimingOverlayState : ISpiderState
    {
        private readonly IMagnetFreezingService _magnetFreezingService;
        private readonly IPlatformObjectsService _platformObjects;
        private readonly ISpiderStateMachine _stateMachine;
        private readonly IInputService _inputService;
        private readonly SpiderStateContext _stateContext;

        private StateMachineData Data => _stateContext.Data;
        private Transform RotationPlaneTransform => _stateContext.RotationPlaneTransform;
        private TrajectoryRender TrajectoryRender => _stateContext.TrajectoryRender;

        private Tween _localMoveTween;

        public AimingOverlayState(
            ISpiderStateMachine stateMachine,
            SpiderStateContext stateContext,
            IMagnetFreezingService magnetFreezingService,
            IPlatformObjectsService platformObjects,
            IInputService inputService)
        {
            _magnetFreezingService = magnetFreezingService;
            _platformObjects = platformObjects;
            _stateMachine = stateMachine;
            _inputService = inputService;
            _stateContext = stateContext;
        }

        public void Enter()
        {
            Data.IsAimingState = true;
            TrajectoryRender.Show();

            _magnetFreezingService.FreezeForAiming();

            _localMoveTween?.Kill();
            _localMoveTween = RotationPlaneTransform.DOLocalMove(Vector3.zero, 0.5f);
        }

        public void Exit()
        {
            Data.IsAimingState = false;

            TrajectoryRender.Hide();
        }

        public void HandleInput()
        {
        }

        public void Update()
        {
            if (_inputService.CenterMouseUp)
            {
                ThrowAllObjects();
                _stateMachine.SwitchState<EmptyOverlayState>();
            }

            TrajectoryRender.FollowTrajectory(RotationPlaneTransform.position, RotationPlaneTransform.up * 10);
        }

        public void FixedUpdate()
        {
        }

        public void LateUpdate()
        {
        }

        private void ThrowAllObjects()
        {
            _magnetFreezingService.UnfreezeForAiming();

            Vector3 targetPosition = RotationPlaneTransform.localPosition;
            targetPosition.y = 0f;

            _platformObjects.ThrowAll();

            _localMoveTween?.Kill();
            _localMoveTween = RotationPlaneTransform.DOLocalMove(targetPosition, 0.05f);
        }
    }
}