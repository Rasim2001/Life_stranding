using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Ground;
using UnityEngine;

namespace SpiderController.StateMachine.States.Airborn
{
    public class JumpingState : AirbornState
    {
        private readonly GroundChecker _spiderGroundChecker;
        private float _offsetJumpingTime;

        public JumpingState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(stateMachine, inputService,
            staticDataService, spider,
            stateMachineData, legs, flower, energySystem)
        {
            _spiderGroundChecker = spider.GroundChecker;
        }

        public override void Enter()
        {
            base.Enter();

            Data.AirbornSpeed = SpiderStaticData.FallSpeed;
            Data.YVelocity = SpiderStaticData.StartYVelocity;

            Flower.IsFreezingOnPlatform = true;

            _offsetJumpingTime = 0.5f;
        }


        public override void Exit()
        {
            base.Exit();

            Flower.IsFreezingOnPlatform = false;
        }

        public override void Update()
        {
            base.Update();

            EnergySystem.SpendEnergy(SpiderStaticData.EnergySpendAirbornSpeed);

            if (InputService.JumpUp)
                StateMachine.SwitchState<FallingWithControlState>();

            if (Data.EnergyFillAmount <= 0)
                StateMachine.SwitchState<FallingWithoutEnergyState>();

            if (Data.YVelocity < 0)
                StateMachine.SwitchState<FallingState>();

            if (_offsetJumpingTime > 0)
                _offsetJumpingTime -= Time.deltaTime;

            if (_offsetJumpingTime <= 0 && _spiderGroundChecker.IsTouchesWithLegs)
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