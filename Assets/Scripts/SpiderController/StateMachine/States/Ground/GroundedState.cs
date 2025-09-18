using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Airborn;

namespace SpiderController.StateMachine.States.Ground
{
    public class GroundedState : MovementState
    {
        private readonly GroundChecker _groundChecker;

        protected GroundedState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(stateMachine, inputService,
            staticDataService, spider,
            stateMachineData, legs, flower, energySystem)
        {
            _groundChecker = spider.GroundChecker;
        }


        public override void Update()
        {
            base.Update();

            if (_groundChecker.IsTouchesWithLegs == false)
                StateMachine.SwitchState<FallingState>();

            if (InputService.JumpPressed && Data.EnergyFillAmount > 0)
                StateMachine.SwitchState<JumpingState>();

            if (InputService.JerkPressed && Data.EnergyFillAmount > 0)
                StateMachine.SwitchState<JerkState>();
        }
    }
}