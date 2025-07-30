using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController.StateMachine.States.Ground
{
    public class IdlingState : GroundedState
    {
        public IdlingState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs) : base(stateMachine, inputService, staticDataService, spider, stateMachineData, legs)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;

            Data.Speed = 0;
        }


        public override void Update()
        {
            base.Update();

            RestoreEnergy(SpiderStaticData.EnergyFillSpeed);

            if (IsInputZero())
                return;

            StateMachine.SwitchState<RunningState>();
        }
    }
}