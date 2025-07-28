using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Ground;

namespace SpiderController.StateMachine.States.Airborn
{
    public class FallingState : AirbornState
    {
        private readonly GroundChecker _spiderGroundChecker;

        public FallingState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs) : base(stateMachine, inputService, staticDataService, spider, stateMachineData, legs)
        {
            _spiderGroundChecker = spider.GroundChecker;
        }

        public override void Update()
        {
            base.Update();

            if (_spiderGroundChecker.IsTouches)
            {
                Data.YVelocity = 0;

                if (IsInputZero())
                    StateMachine.SwitchState<IdlingState>();
                else
                    StateMachine.SwitchState<RunningState>();
            }
        }
    }
}