using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using SpiderController.SpiderMove;
using SpiderController.UI;

namespace SpiderController.StateMachine.States.Ground
{
    public class FastRunningState : GroundedState
    {
        public FastRunningState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(stateMachine, inputService,
            staticDataService, spider,
            stateMachineData, legs, flower, energySystem)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Data.Speed = SpiderStaticData.FastSpeed;
            Data.DistanceFromGround = SpiderStaticData.DistanceFromGround;

            ApplyFastRunning();

            EnergyBarUI.ShowHologram();
        }

        public override void Exit()
        {
            base.Exit();

            ApplyDefaultSpeed();
        }

        public override void Update()
        {
            base.Update();

            EnergySystem.SpendEnergy(SpiderStaticData.EnergySpendFastRunningSpeed);

            if (!IsFastRunUp() && Data.EnergyFillAmount > 0)
                return;

            if (IsInputZero())
                StateMachine.SwitchState<IdlingState>();
            else
                StateMachine.SwitchState<RunningState>();
        }

        private void ApplyFastRunning()
        {
            float multiplier = SpiderStaticData.FastSpeed / SpiderStaticData.Speed;
            foreach (LegDataStruct legData in Legs)
                legData.Leg.SetAcceleration(multiplier);
        }

        private void ApplyDefaultSpeed()
        {
            foreach (LegDataStruct legData in Legs)
                legData.Leg.SetDefaultSpeed();
        }
    }
}