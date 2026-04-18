using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CutScene;
using Infastructure.Services.Pause;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using SpiderController.TriggerChecker;


namespace SpiderController.StateMachine.States.Ground
{
    public class RunningState : GroundedState
    {
        protected RunningState(
            ISpiderStateMachine stateMachine,
            SpiderServiceContext serviceContext,
            SpiderStateContext stateContext,
            EnergySystem energySystem) :
            base(stateMachine, serviceContext, stateContext, energySystem)
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
            else if (IsFastRunPressed() && AbilityService.IsExploredAbility(ProductType.FastRunSkillProduct))
                StateMachine.SwitchState<FastRunningState>();
        }
    }
}