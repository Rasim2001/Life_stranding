using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CutScene;
using Infastructure.Services.Pause;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.SpiderMove;
using SpiderController.TriggerChecker;

namespace SpiderController.StateMachine.States.Ground
{
    public class FastRunningState : GroundedState
    {
        protected FastRunningState(
            ISpiderStateMachine stateMachine,
            SpiderServiceContext serviceContext,
            SpiderStateContext stateContext,
            EnergySystem energySystem) :
            base(stateMachine, serviceContext, stateContext, energySystem)
        {
        }

        public override void Enter()
        {
            base.Enter();

            WindowService.OnWindowOpened += ReturnToNormalMovement;

            Data.OnTotalWeightChanged += WeightChanged;
            Data.DistanceFromGround = SpiderStaticData.DistanceFromGround;

            SetSpeed(SpiderStaticData.FastSpeed);
            ApplyFastRunning();

            EnergyBarUI.ShowHologram();
        }

        public override void Exit()
        {
            base.Exit();

            WindowService.OnWindowOpened -= ReturnToNormalMovement;

            Data.OnTotalWeightChanged -= WeightChanged;

            ApplyDefaultSpeed();
        }

        private void WeightChanged() =>
            SetSpeed(SpiderStaticData.FastSpeed);

        public override void Update()
        {
            base.Update();

            EnergySystem.SpendEnergy(SpiderStaticData.EnergySpendFastRunningSpeed);

            if (!IsFastRunUp() && Data.CurrentEnergyFillAmount > 0)
                return;

            ReturnToNormalMovement();
        }

        private void ReturnToNormalMovement()
        {
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