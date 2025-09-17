using System;
using Cysharp.Threading.Tasks;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Ground;
using SpiderController.UI;
using SpiderController.UI.Stickers;

namespace SpiderController.StateMachine.States.Airborn
{
    public class FallingWithoutEnergyState : AirbornState
    {
        private StickerUI StickerUI => Spider.SpiderUI.StickerUI;

        private readonly GroundChecker _spiderGroundChecker;

        public FallingWithoutEnergyState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(stateMachine, inputService,
            staticDataService, spider,
            stateMachineData, legs, flower, energySystem)
        {
            _spiderGroundChecker = spider.GroundChecker;
        }

        public override void Enter()
        {
            base.Enter();

            Data.AirbornSpeed = SpiderStaticData.FallWithoutEnergySpeed;
            Data.IsFallingDownWithoutEnergyState = true;

            EnergyBarUI.ShowHologram();
        }

        public override void Exit()
        {
            base.Exit();

            StickerUI.PlaySticker(StickerEnum.FallingDown);
            Data.IsFallingDownWithoutEnergyState = false;
        }


        public override void Update()
        {
            base.Update();

            EnergySystem.SpendEnergy(SpiderStaticData.EnergySpendAirbornSpeed);

            if (_spiderGroundChecker.IsTouchesWithLegs)
                StandUpAsync().Forget();
        }


        private async UniTask StandUpAsync()
        {
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