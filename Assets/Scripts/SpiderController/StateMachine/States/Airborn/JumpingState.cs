using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;

namespace SpiderController.StateMachine.States.Airborn
{
    public class JumpingState : AirbornState
    {
        private readonly GroundChecker _spiderGroundChecker;

        public JumpingState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower) : base(stateMachine, inputService, staticDataService, spider,
            stateMachineData, legs, flower)
        {
            _spiderGroundChecker = spider.GroundChecker;
        }

        public override void Enter()
        {
            base.Enter();

            Data.AirbornSpeed = SpiderStaticData.FallSpeed;
            Data.YVelocity = SpiderStaticData.StartYVelocity;
        }

        public override void Update()
        {
            base.Update();

            if (Data.EnergyFillAmount <= 0)
                StateMachine.SwitchState<FallingWithoutEnergyState>();

            if (Data.YVelocity < 0 || _spiderGroundChecker.IsTouchingGround)
                StateMachine.SwitchState<FallingState>();
        }
    }
}