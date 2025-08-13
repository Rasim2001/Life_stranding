using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;

namespace SpiderController.StateMachine.States.Ground
{
    public class FastRunningState : GroundedState
    {
        public FastRunningState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower) : base(stateMachine, inputService, staticDataService, spider,
            stateMachineData, legs, flower)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Data.Speed = SpiderStaticData.FastSpeed;
            Data.DistanceFromGround = SpiderStaticData.DistanceFromGround;

            ApplyFastRunning();
        }

        public override void Exit()
        {
            base.Exit();

            ApplyDefaultSpeed();
        }

        public override void Update()
        {
            base.Update();

            SpendEnergy(SpiderStaticData.EnergySpendFastRunningSpeed);

            if (!IsFastRunUp() && Data.EnergyFillAmount > 0)
                return;

            if (IsInputZero())
                StateMachine.SwitchState<IdlingState>();
            else
                StateMachine.SwitchState<RunningState>();
        }

        private void ApplyFastRunning()
        {
            float multiplier = SpiderStaticData.FastSpeed / SpiderStaticData.Speed;
            foreach (LegDataStruct legData in Legs)
                legData.Leg.SetAcceleration(multiplier);
        }

        private void ApplyDefaultSpeed()
        {
            foreach (LegDataStruct legData in Legs)
                legData.Leg.SetDefaultSpeed();
        }
    }
}