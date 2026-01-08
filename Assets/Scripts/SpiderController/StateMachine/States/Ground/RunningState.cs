using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController.StateMachine.States.Ground
{
    public class RunningState : GroundedState
    {
        public RunningState(ISpiderStateMachine stateMachine,
            IInputService inputService,
            IStaticDataService staticDataService,
            ICutSceneService cutSceneService,
            Spider spider,
            StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower,
            EnergySystem energySystem) : base(stateMachine,
            inputService,
            staticDataService,
            cutSceneService,
            spider,
            stateMachineData,
            legs,
            flower, energySystem)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Data.OnTotalWeightChanged += WeightChanged;
            Data.DistanceFromGround = SpiderStaticData.DistanceFromGround;

            SetSpeed(SpiderStaticData.Speed);
            EnergyBarUI.PlayFadeHologramEffect();
        }

        public override void Exit()
        {
            base.Exit();

            Data.OnTotalWeightChanged -= WeightChanged;
        }

        private void WeightChanged() =>
            SetSpeed(SpiderStaticData.Speed);

        public override void Update()
        {
            base.Update();

            if (!Data.IsMouseHolding)
                EnergySystem.RestoreEnergy(SpiderStaticData.EnergyFillSpeed);

            if (SlowdownPressed())
                StateMachine.SwitchState<SlowdownState>();
            else if (IsInputZero())
                StateMachine.SwitchState<IdlingState>();
            else if (IsFastRunPressed() && Spider.AbilityService.IsExploredAbility(ProductType.FastRunSkillProduct))
                StateMachine.SwitchState<FastRunningState>();
        }
    }
}