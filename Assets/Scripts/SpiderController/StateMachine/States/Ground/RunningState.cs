using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController.StateMachine.States.Ground
{
    public class RunningState : GroundedState
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

            RestoreEnergy();

            if (IsInputZero())
                StateMachine.SwitchState<IdlingState>();
            else if (IsFastRunPressed())
                StateMachine.SwitchState<FastRunningState>();
        }
    }
}