using SpiderController.StateMachine.States.Ground;

namespace SpiderController.StateMachine.States.Airborn
{
    public class FallingState : AirbornState
    {
        protected FallingState(
            ISpiderStateMachine stateMachine,
            SpiderServiceContext serviceContext,
            SpiderStateContext stateContext,
            EnergySystem energySystem) : base(stateMachine, serviceContext, stateContext, energySystem)
        {
        }

        public override void Enter()
        {
            base.Enter();

            WaterObserverTrigger.OnTriggerEnterHappened += OnTriggerEnterWithWater;

            Data.YVelocity = 0;
            Data.GlobalY = Transform.position.y;
            Data.AirbornSpeed = SpiderStaticData.FallSpeed;

            MagnetFreezingService.Freeze();
            ThrusterSystem.Open(true);

            EnergyBarUI.ShowHologram();
        }

        public override void Exit()
        {
            base.Exit();

            WaterObserverTrigger.OnTriggerEnterHappened -= OnTriggerEnterWithWater;

            ThrusterSystem.Open(false);
            MagnetFreezingService.Unfreeze();
        }


        public override void Update()
        {
            base.Update();

            EnergySystem.SpendEnergy(SpiderStaticData.EnergySpendAirbornSpeed);

            if (InputService.JumpUp)
                StateMachine.SwitchState<FallingWithControlState>();

            if (Data.CurrentEnergyFillAmount <= 0)
                StateMachine.SwitchState<FallingWithoutEnergyState>();

            if (SpiderGroundChecker.IsTouchesWithLegs)
            {
                ShakeCamera();

                Data.YVelocity = 0;

                if (IsInputZero())
                    StateMachine.SwitchState<IdlingState>();
                else
                    StateMachine.SwitchState<RunningState>();
            }
        }
    }
}