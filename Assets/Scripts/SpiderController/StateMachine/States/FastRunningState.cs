using _2;
using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;

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

            float multiplier = SpiderStaticData.FastSpeed / SpiderStaticData.Speed;
            foreach (LegDataStruct legData in Legs)
                legData.Leg.SetAcceleration(multiplier);
        }

        public override void Exit()
        {
            base.Exit();

            foreach (LegDataStruct legData in Legs)
                legData.Leg.SetDefaultSpeed();
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
    }
}