using DG.Tweening;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController.StateMachine.States.Ground
{
    public class SlowdownState : GroundedState
    {
        private readonly Vector3 _defaultPosition;

        private Tween _localMoveTween;

        public SlowdownState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, ICutSceneService cutSceneService, Spider spider,
            StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(stateMachine, inputService,
            staticDataService, cutSceneService, spider,
            stateMachineData, legs, flower, energySystem)
        {
            _defaultPosition = Spider.RotationPlaneTransform.localPosition;
        }

        public override void Enter()
        {
            base.Enter();

            Data.DistanceFromGround = SpiderStaticData.SlowdownDistanceFromGround;
            Data.Speed = SpiderStaticData.SlowdownSpeed;

            _localMoveTween?.Kill();
            _localMoveTween = Spider.RotationPlaneTransform.DOLocalMove(Vector3.zero, 0.5f);

            EnergyBarUI.PlayFadeHologramEffect();
        }

        public override void Exit()
        {
            base.Exit();

            _localMoveTween?.Kill();
            _localMoveTween = Spider.RotationPlaneTransform.DOLocalMove(_defaultPosition, 0.5f);
        }

        public override void Update()
        {
            base.Update();

            if (!Data.IsMouseHolding)
                EnergySystem.RestoreEnergy(SpiderStaticData.EnergyFillSpeed);

            if (!SlowdownUp())
                return;

            if (IsInputZero())
                StateMachine.SwitchState<IdlingState>();
            else if (IsFastRunPressed())
                StateMachine.SwitchState<FastRunningState>();
            else
                StateMachine.SwitchState<RunningState>();
        }
    }
}