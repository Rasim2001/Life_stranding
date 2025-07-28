using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController.StateMachine.States.Airborn
{
    public class JumpingState : AirbornState
    {
        public JumpingState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs) : base(stateMachine, inputService, staticDataService, spider, stateMachineData, legs)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Data.YVelocity = SpiderStaticData.StartYVelocity;
        }

        public override void Update()
        {
            base.Update();

            if (Data.YVelocity < 0)
                StateMachine.SwitchState<FallingState>();
        }
    }
}