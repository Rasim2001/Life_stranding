using Infastructure.Services.Input;
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
            LegDataStruct[] legs, Flower flower) : base(stateMachine, inputService, staticDataService, spider,
            stateMachineData, legs, flower)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Data.DistanceFromGround = SpiderStaticData.DistanceFromGround;
            ApplyJerkSpeed();

            _dashTimer = SpiderStaticData.JerkDuration;
        }

        public override void Exit()
        {
            base.Exit();

            ApplyDefaultSpeed();
        }

        public override void Update()
        {
            SpendEnergy(SpiderStaticData.EnergySpendJerkingSpeed);
            UpdateDashTime();
            UpdateJerpVelocity();

            if (_dashTimer <= 0 || Data.EnergyFillAmount <= 0)
                SwitchState();
        }

        private void UpdateDashTime() =>
            _dashTimer -= Time.deltaTime;

        private void UpdateJerpVelocity()
        {
            float dashProgress = 1f - _dashTimer / SpiderStaticData.JerkDuration;
            float currentDashSpeed = SpiderStaticData.JerkSpeed * SpiderStaticData.JerkCurve.Evaluate(dashProgress);

            Data.XVelocity = currentDashSpeed;
        }

        private void SwitchState()
        {
            Data.XVelocity = 0;

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