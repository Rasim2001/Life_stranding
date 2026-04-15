using System;
using Cysharp.Threading.Tasks;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Ground;
using SpiderController.UI.Stickers;
using UnityEngine;

namespace SpiderController.StateMachine.States.Airborn
{
    public class FallingWithoutEnergyState : AirbornState
    {
        protected FallingWithoutEnergyState(
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

            Data.GlobalY = Transform.position.y;
            Data.AirbornSpeed = SpiderStaticData.FallWithoutEnergySpeed;
            Data.IsFallingDownWithoutEnergyState = true;

            EnergyBarUI.ShowHologram();

            SetCrossLegs();
        }


        public override void Exit()
        {
            base.Exit();

            WaterObserverTrigger.OnTriggerEnterHappened -= OnTriggerEnterWithWater;

            Stickers.PlaySticker(StickerEnum.FallingDown);
            Data.IsFallingDownWithoutEnergyState = false;

            SetUncrossLegs();
        }


        public override void Update()
        {
            base.Update();

            EnergySystem.SpendEnergy(SpiderStaticData.EnergySpendAirbornSpeed);

            if (SpiderGroundChecker.IsTouchesWithLegs)
                StandUpAsync().Forget();
        }

        protected override Vector3 GetMovementY() =>
            Vector3.up * Data.YVelocity;


        private async UniTask StandUpAsync()
        {
            ShakeCamera();

            Data.IsStandingUpAfterFalling = true;

            if (IsInputZero())
                StateMachine.SwitchState<IdlingState>();
            else
                StateMachine.SwitchState<RunningState>();

            Data.YVelocity = 0;

            foreach (LegDataStruct legData in Legs)
                legData.Leg.SetAcceleration(0.5f);

            await UniTask.Delay(TimeSpan.FromSeconds(2));

            foreach (LegDataStruct legData in Legs)
                legData.Leg.SetDefaultSpeed();

            Data.IsStandingUpAfterFalling = false;
        }
    }
}