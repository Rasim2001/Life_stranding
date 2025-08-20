using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController.StateMachine.States.Ground
{
    public class RunningState : GroundedState
    {
        public RunningState(ISpiderStateMachine stateMachine,
            IInputService inputService,
            IStaticDataService staticDataService,
            Spider spider,
            StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower) : base(stateMachine,
            inputService,
            staticDataService,
            spider,
            stateMachineData,
            legs,
            flower)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Data.DistanceFromGround = SpiderStaticData.DistanceFromGround;
            Data.Speed = SpiderStaticData.Speed;
        }

        public override void Update()
        {
            base.Update();

            if (!Data.IsMouseHolding)
                RestoreEnergy(SpiderStaticData.EnergyFillSpeed);

            if (SlowdownPressed())
                StateMachine.SwitchState<SlowdownState>();
            else if (IsInputZero())
                StateMachine.SwitchState<IdlingState>();
            else if (IsFastRunPressed())
                StateMachine.SwitchState<FastRunningState>();
        }
    }
}