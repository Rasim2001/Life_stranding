using SpiderController.StateMachine.States.Ground;
using UnityEngine;

namespace SpiderController.StateMachine.States.Airborn
{
    public class JumpingState : AirbornState
    {
        private float _offsetJumpingTime;

        protected JumpingState(
            ISpiderStateMachine stateMachine,
            SpiderServiceContext serviceContext,
            SpiderStateContext stateContext,
            EnergySystem energySystem) : base(stateMachine, serviceContext, stateContext, energySystem)
        {
        }

        public override void Enter()
        {
            base.Enter();

            WindowService.OnWindowOpened += GoToFallingWithControlState;

            Data.AirbornSpeed = SpiderStaticData.FallSpeed;
            Data.YVelocity = SpiderStaticData.StartYVelocity;

            MagnetFreezingService.Freeze();
            ThrusterSystem.Open(true);

            _offsetJumpingTime = 0.5f;

            EnergyBarUI.ShowHologram();
        }


        public override void Exit()
        {
            base.Exit();

            WindowService.OnWindowOpened -= GoToFallingWithControlState;

            ThrusterSystem.Open(false);
            MagnetFreezingService.Unfreeze();
        }

        public override void Update()
        {
            base.Update();

            EnergySystem.SpendEnergy(SpiderStaticData.EnergySpendAirbornSpeed);

            if (InputService.JumpUp)
                GoToFallingWithControlState();

            if (Data.CurrentEnergyFillAmount <= 0)
                StateMachine.SwitchState<FallingWithoutEnergyState>();

            if (Data.YVelocity < 0)
                StateMachine.SwitchState<FallingState>();

            if (_offsetJumpingTime > 0)
                _offsetJumpingTime -= Time.deltaTime;

            if (_offsetJumpingTime <= 0 && SpiderGroundChecker.IsTouchesWithLegs)
            {
                Data.GlobalY = 0;
                Data.YVelocity = 0;

                if (IsInputZero())
                    StateMachine.SwitchState<IdlingState>();
                else
                    StateMachine.SwitchState<RunningState>();
            }
        }


        private void GoToFallingWithControlState() =>
            StateMachine.SwitchState<FallingWithControlState>();
    }
}