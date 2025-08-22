using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;

namespace SpiderController.StateMachine.States.Ground
{
    public class SlowdownState : GroundedState
    {
        public SlowdownState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(stateMachine, inputService,
            staticDataService, spider,
            stateMachineData, legs, flower, energySystem)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Data.DistanceFromGround = SpiderStaticData.SlowdownDistanceFromGround;
            Data.Speed = SpiderStaticData.SlowdownSpeed;
        }

        public override void Update()
        {
            base.Update();

            if (!Data.IsMouseHolding)
                EnergySystem.RestoreEnergy(SpiderStaticData.EnergyFillSpeed);

            if (!SlowdownUp())
                return;

            if (IsInputZero())
                StateMachine.SwitchState<IdlingState>();
            else if (IsFastRunPressed())
                StateMachine.SwitchState<FastRunningState>();
            else
                StateMachine.SwitchState<RunningState>();
        }
    }
}