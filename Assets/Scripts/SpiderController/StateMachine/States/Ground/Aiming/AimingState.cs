using System.Linq;
using DG.Tweening;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController.StateMachine.States.Ground.Aiming
{
    public class AimingState : GroundedState
    {
        private readonly IPlatformObjectsService _platformObjectsService;
        private readonly Vector3 _defaultPosition;

        private Tween _localMoveTween;

        protected AimingState(
            ISpiderStateMachine stateMachine,
            IInputService inputService,
            IStaticDataService staticDataService,
            ICutSceneService cutSceneService,
            IPlatformObjectsService platformObjectsService,
            Spider spider,
            StateMachineData stateMachineData,
            LegDataStruct[] legs,
            Flower flower,
            EnergySystem energySystem) : base(
            stateMachine,
            inputService,
            staticDataService,
            cutSceneService,
            spider,
            stateMachineData,
            legs,
            flower,
            energySystem)
        {
            _platformObjectsService = platformObjectsService;
            _defaultPosition = Spider.RotationPlaneTransform.localPosition;
        }

        public override void Enter()
        {
            base.Enter();

            Data.AimingStateChanged += OnAimingStateChanged;

            if (Data.IsInAimingState == false)
            {
                Spider.MagnetFreezingService.FreezeForAiming();
                Data.IsInAimingState = true;

                _localMoveTween?.Kill();
                _localMoveTween = Spider.RotationPlaneTransform.DOLocalMove(Vector3.zero, 0.5f);
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
            Spider.MagnetFreezingService.UnfreezeForAiming();

            Vector3 targetPosition = Spider.RotationPlaneTransform.localPosition;
            targetPosition.y = _defaultPosition.y;

            int count = _platformObjectsService.PickupObjects.Count;

            for (int i = 0; i < count; i++)
                _platformObjectsService.Throw(_platformObjectsService.PickupObjects[i]);

            _localMoveTween?.Kill();
            _localMoveTween = Spider.RotationPlaneTransform.DOLocalMove(targetPosition, 0.05f);
        }
    }
}