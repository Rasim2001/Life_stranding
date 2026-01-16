using DG.Tweening;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
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

        protected AimingState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, ICutSceneService cutSceneService,
            IPlatformObjectsService platformObjectsService, Spider spider,
            StateMachineData stateMachineData, LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(
            stateMachine, inputService, staticDataService, cutSceneService, spider, stateMachineData, legs, flower,
            energySystem)
        {
            _platformObjectsService = platformObjectsService;
            _defaultPosition = Spider.RotationPlaneTransform.localPosition;
        }

        public override void Enter()
        {
            base.Enter();

            Spider.MagnetFreezingService.FreezeForAiming();

            if (Data.IsInAimingState == false)
            {
                Data.IsInAimingState = true;

                _localMoveTween?.Kill();
                _localMoveTween = Spider.RotationPlaneTransform.DOLocalMove(Vector3.zero, 0.5f);
            }
        }

        public override void Exit()
        {
            base.Exit();

            Spider.MagnetFreezingService.UnfreezeForAiming();
        }

        public override void Update()
        {
            base.Update();

            if (InputService.CenterMouseUp)
            {
                Data.IsInAimingState = false;

                ThrowAllObjects();

                if (IsInputZero())
                    StateMachine.SwitchState<IdlingState>();
                else
                    StateMachine.SwitchState<RunningState>();

                Vector3 targetPosition = Spider.RotationPlaneTransform.localPosition;
                targetPosition.y = _defaultPosition.y;

                _localMoveTween?.Kill();
                _localMoveTween = Spider.RotationPlaneTransform.DOLocalMove(targetPosition, 0.05f);
            }
        }

        private void ThrowAllObjects()
        {
            int count = _platformObjectsService.PickupObjects.Count;

            for (int i = 0; i < count; i++)
                _platformObjectsService.PickupObjects[i].GetComponent<IThrowable>().ThrowObject();
        }
    }
}