using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Ground;
using UnityEngine;

namespace SpiderController.StateMachine.States.Airborn
{
    public class FallingWithoutEnergyState : AirbornState
    {
        private readonly GroundChecker _spiderGroundChecker;

        public FallingWithoutEnergyState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs) : base(stateMachine, inputService, staticDataService, spider, stateMachineData, legs)
        {
            _spiderGroundChecker = spider.GroundChecker;
        }

        public override void Enter()
        {
            base.Enter();

            Data.AirbornSpeed = SpiderStaticData.FallWithoutEnergySpeed;
        }

        public override void Update()
        {
            base.Update();

            if (_spiderGroundChecker.IsTouchingGround)
                Spider.transform.localEulerAngles = Vector3.zero;

            if (_spiderGroundChecker.IsTouchesWithLegs || _spiderGroundChecker.IsTouchingGround)
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