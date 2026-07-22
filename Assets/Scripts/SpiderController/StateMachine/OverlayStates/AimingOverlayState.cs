using DG.Tweening;
using Infastructure.Services.Magnet;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.Spider;
using SpiderController.Trajectory;
using SpiderController.UI;
using SpiderController.UI.Health;
using UnityEngine;

namespace SpiderController.StateMachine.OverlayStates
{
    public class AimingOverlayState : ISpiderState
    {
        private readonly IMagnetFreezingService _magnetFreezingService;
        private readonly IPlatformObjectsService _platformObjects;
        private readonly ISpiderStateMachine _stateMachine;
        private readonly SpiderServiceContext _serviceContext;
        private readonly IInputService _inputService;
        private readonly SpiderStateContext _stateContext;
        private readonly EnergySystem _energySystem;

        private SpiderStaticData SpiderStaticData => _serviceContext.StaticDataService.SpiderStaticData;
        private StateMachineData Data => _stateContext.Data;
        private Transform RotationPlaneTransform => _stateContext.RotationPlaneTransform;
        private TrajectoryRender TrajectoryRender => _stateContext.TrajectoryRender;

        private EnergyBarUI EnergyBarUI => _stateContext.SpiderUI.EnergyBar;

        private Tween _localMoveTween;

        public AimingOverlayState(
            ISpiderStateMachine stateMachine,
            SpiderStateContext stateContext,
            SpiderServiceContext serviceContext,
            EnergySystem energySystem,
            IMagnetFreezingService magnetFreezingService,
            IPlatformObjectsService platformObjects,
            IInputService inputService)
        {
            _magnetFreezingService = magnetFreezingService;
            _platformObjects = platformObjects;
            _stateMachine = stateMachine;
            _serviceContext = serviceContext;
            _inputService = inputService;
            _stateContext = stateContext;
            _energySystem = energySystem;
        }

        public void Enter()
        {
            Data.IsAimingState = true;
            TrajectoryRender.Show();
            EnergyBarUI.ShowHologram(this);

            _magnetFreezingService.FreezeForAiming();

            _energySystem.LockRestore(this);
            _localMoveTween?.Kill();
            _localMoveTween = RotationPlaneTransform.DOLocalMove(Vector3.zero, 0.5f);
        }

        public void Exit()
        {
            Data.IsAimingState = false;
            TrajectoryRender.Hide();
            EnergyBarUI.PlayFadeHologramEffect(this);

            _energySystem.UnlockRestore(this);
        }

        public void HandleInput()
        {
        }

        public void Update()
        {
            _energySystem.SpendEnergy(SpiderStaticData.EnergyAimingSpeed);

            if (_inputService.CenterMouseUp || Data.CurrentEnergyFillAmount <= 0)
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