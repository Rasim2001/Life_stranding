using _2;
using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;

namespace SpiderController.StateMachine.States
{
    public class FastRunningState : MovementState
    {
        public FastRunningState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs) : base(stateMachine, inputService, staticDataService, spider, stateMachineData, legs)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Data.Speed = SpiderStaticData.FastSpeed;

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

            if (!IsFastRunUp())
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