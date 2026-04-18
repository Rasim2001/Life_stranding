using DG.Tweening;
using PickupObjects;
using UnityEngine;

namespace SpiderController.StateMachine.States.Ground
{
    public class SlowdownState : GroundedState
    {
        private readonly Vector3 _defaultPosition;
        private Transform RotationPlaneTransform => StateContext.RotationPlaneTransform;

        private Tween _localMoveTween;

        protected SlowdownState(
            ISpiderStateMachine stateMachine,
            SpiderServiceContext serviceContext,
            SpiderStateContext stateContext,
            EnergySystem energySystem) :
            base(stateMachine, serviceContext, stateContext, energySystem)
        {
            _defaultPosition = RotationPlaneTransform.localPosition;
        }


        public override void Enter()
        {
            base.Enter();

            WindowService.OnWindowOpened += ReturnToNormalMovement;
            Data.OnTotalWeightChanged += WeightChanged;
            Data.DistanceFromGround = SpiderStaticData.SlowdownDistanceFromGround;
            SetSpeed(SpiderStaticData.SlowdownSpeed);

            _localMoveTween?.Kill();
            _localMoveTween = RotationPlaneTransform.DOLocalMove(Vector3.zero, 0.5f);

            EnergyBarUI.PlayFadeHologramEffect();
        }

        public override void Exit()
        {
            base.Exit();

            WindowService.OnWindowOpened -= ReturnToNormalMovement;
            Data.OnTotalWeightChanged -= WeightChanged;

            _localMoveTween?.Kill();
            _localMoveTween = RotationPlaneTransform.DOLocalMove(_defaultPosition, 0.5f);
        }

        public override void Update()
        {
            base.Update();

            if (!Data.IsMouseHolding)
                EnergySystem.RestoreEnergy(SpiderStaticData.EnergyFillSpeed);

            if (!SlowdownUp())
                return;

            ReturnToNormalMovement();
        }

        private void ReturnToNormalMovement()
        {
            if (IsInputZero())
                StateMachine.SwitchState<IdlingState>();
            else if (IsFastRunPressed() && AbilityService.IsExploredAbility(ProductType.FastRunSkillProduct))
                StateMachine.SwitchState<FastRunningState>();
            else
                StateMachine.SwitchState<RunningState>();
        }

        private void WeightChanged() =>
            SetSpeed(SpiderStaticData.SlowdownSpeed);
    }
}