using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController.StateMachine.States.Ground
{
    public class JerkState : GroundedState
    {
        private float _dashTimer;

        public JerkState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(stateMachine, inputService,
            staticDataService, spider,
            stateMachineData, legs, flower, energySystem)
        {
        }

        public override void Enter()
        {
            base.Enter();

            ApplyJerkSpeed();

            Data.DistanceFromGround = SpiderStaticData.DistanceFromGround;
            _dashTimer = SpiderStaticData.JerkDuration;
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

            if (_dashTimer <= 0 || Data.EnergyFillAmount <= 0)
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