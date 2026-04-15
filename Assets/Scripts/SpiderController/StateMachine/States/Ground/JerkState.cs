using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CutScene;
using Infastructure.Services.Pause;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using SpiderController.TriggerChecker;
using SpiderController.UI;
using UnityEngine;

namespace SpiderController.StateMachine.States.Ground
{
    public class JerkState : GroundedState
    {
        private float _dashTimer;

        protected JerkState(
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

            ApplyJerkSpeed();

            Data.DistanceFromGround = SpiderStaticData.DistanceFromGround;
            _dashTimer = SpiderStaticData.JerkDuration;

            EnergyBarUI.ShowHologram();
        }

        public override void Exit()
        {
            base.Exit();

            Data.XVelocity = 0;

            ApplyDefaultSpeed();
        }

        public override void Update()
        {
            EnergySystem.SpendEnergy(SpiderStaticData.EnergySpendJerkingSpeed);

            UpdateDashTime();
            UpdateJerkVelocity();

            if (_dashTimer <= 0 || Data.CurrentEnergyFillAmount <= 0)
                SwitchState();
        }

        private void UpdateDashTime() =>
            _dashTimer -= Time.deltaTime;

        private void UpdateJerkVelocity()
        {
            float dashProgress = 1f - _dashTimer / SpiderStaticData.JerkDuration;
            float currentDashSpeed = SpiderStaticData.JerkSpeed * SpiderStaticData.JerkCurve.Evaluate(dashProgress);

            Data.XVelocity = currentDashSpeed;
        }

        private void SwitchState()
        {
            if (IsInputZero())
                StateMachine.SwitchState<IdlingState>();
            else
                StateMachine.SwitchState<RunningState>();
        }

        private void ApplyJerkSpeed()
        {
            float multiplier = SpiderStaticData.JerkSpeed / SpiderStaticData.Speed;
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