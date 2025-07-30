using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Airborn;
using UnityEngine;

namespace SpiderController.StateMachine.States.Ground
{
    public class GroundedState : MovementState
    {
        private readonly GroundChecker _groundChecker;

        protected GroundedState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs) : base(stateMachine, inputService, staticDataService, spider, stateMachineData, legs)
        {
            _groundChecker = spider.GroundChecker;
        }

        public override void Enter()
        {
            base.Enter();

            _groundChecker.SetGroundLegState();
        }


        public override void Update()
        {
            base.Update();

            if (_groundChecker.IsTouchesWithLegs == false)
                StateMachine.SwitchState<FallingState>();

            if (InputService.JumpPressed)
                StateMachine.SwitchState<JumpingState>();

            if (InputService.JerkPressed)
                StateMachine.SwitchState<JerkState>();
        }
    }
}