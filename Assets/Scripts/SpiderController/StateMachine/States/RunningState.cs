using _2;
using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;

namespace SpiderController.StateMachine.States
{
    public class RunningState : MovementState
    {
        public RunningState(
            ISpiderStateMachine stateMachine,
            IInputService inputService,
            IStaticDataService staticDataService,
            Spider spider,
            StateMachineData stateMachineData,
            LegDataStruct[] legs) : base(stateMachine,
            inputService,
            staticDataService,
            spider,
            stateMachineData,
            legs)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Data.Speed = SpiderStaticData.Speed;
        }

        public override void Update()
        {
            base.Update();

            if (IsInputZero())
                StateMachine.SwitchState<IdlingState>();
            else if (IsFastRunPressed())
                StateMachine.SwitchState<FastRunningState>();
        }
    }
}