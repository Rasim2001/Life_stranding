using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CutScene;
using Infastructure.Services.Pause;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Infastructure.StaticData.StaticDataService;
using SpiderController.TriggerChecker;
using SpiderController.UI;

namespace SpiderController.StateMachine.States.Ground
{
    public class IdlingState : GroundedState
    {
        protected IdlingState(
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
            SetSpeed(0);

            EnergyBarUI.PlayFadeHologramEffect();
        }

        public override void Exit()
        {
            base.Exit();

            Data.OnTotalWeightChanged -= WeightChanged;
        }

        private void WeightChanged() =>
            SetSpeed(0);

        public override void Update()
        {
            base.Update();

            if (!Data.IsMouseHolding)
                EnergySystem.RestoreEnergy(SpiderStaticData.EnergyFillSpeed);

            if (SlowdownPressed())
                StateMachine.SwitchState<SlowdownState>();

            if (IsInputZero())
                return;

            StateMachine.SwitchState<RunningState>();
        }
    }
}