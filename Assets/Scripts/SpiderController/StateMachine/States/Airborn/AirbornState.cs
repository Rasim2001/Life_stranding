using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController.StateMachine.States.Airborn
{
    public class AirbornState : MovementState
    {
        private readonly GroundChecker _groundChecker;

        protected AirbornState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs) : base(stateMachine, inputService, staticDataService, spider, stateMachineData, legs)
        {
            _groundChecker = spider.GroundChecker;
        }

        public override void Enter()
        {
            base.Enter();

            _groundChecker.SetAirbornLegState();

            Data.Speed = SpiderStaticData.JumpSpeed;
        }


        public override void Update()
        {
            base.Update();

            Data.YVelocity -= SpiderStaticData.BaseGravity * Time.deltaTime;
        }
    }
}