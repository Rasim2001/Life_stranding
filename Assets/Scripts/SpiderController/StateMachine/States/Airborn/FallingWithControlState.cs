using SpiderController.StateMachine.States.Ground;
using SpiderController.UI.Stickers;

namespace SpiderController.StateMachine.States.Airborn
{
    public class FallingWithControlState : AirbornState
    {
        protected FallingWithControlState(
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

            SetCrossLegs();
        }

        public override void Exit()
        {
            base.Exit();

            WaterObserverTrigger.OnTriggerEnterHappened -= OnTriggerEnterWithWater;

            SetUncrossLegs();
        }


        public override void Update()
        {
            base.Update();

            if (InputService.JumpPressed)
                StateMachine.SwitchState<FallingState>();

            if (SpiderGroundChecker.IsTouchesWithLegs)
            {
                ShakeCamera();

                Data.YVelocity = 0;
                Stickers.PlaySticker(StickerEnum.FallingDown);

                if (IsInputZero())
                    StateMachine.SwitchState<IdlingState>();
                else
                    StateMachine.SwitchState<RunningState>();
            }
        }
    }
}