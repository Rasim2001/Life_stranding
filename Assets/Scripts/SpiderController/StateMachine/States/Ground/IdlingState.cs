using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;

namespace SpiderController.StateMachine.States.Ground
{
    public class IdlingState : GroundedState
    {
        public IdlingState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(stateMachine, inputService,
            staticDataService, spider,
            stateMachineData, legs, flower, energySystem)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Data.Speed = 0;
            Data.DistanceFromGround = SpiderStaticData.DistanceFromGround;

            EnergyBarUI.PlayFadeHologramEffect();
        }


        public override void Update()
        {
            base.Update();

            if (!Data.IsMouseHolding)
                EnergySystem.RestoreEnergy(SpiderStaticData.EnergyFillSpeed);

            if (SlowdownPressed())
                StateMachine.SwitchState<SlowdownState>();

            if (IsInputZero())
                return;

            StateMachine.SwitchState<RunningState>();
        }
    }
}