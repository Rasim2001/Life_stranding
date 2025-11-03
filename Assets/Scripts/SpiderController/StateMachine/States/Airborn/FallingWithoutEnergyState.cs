using System;
using Cysharp.Threading.Tasks;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Ground;
using SpiderController.TriggerChecker;
using SpiderController.UI.Stickers;

namespace SpiderController.StateMachine.States.Airborn
{
    public class FallingWithoutEnergyState : AirbornState
    {
        //private Sticker Sticker => Spider.SpiderUI.Sticker;

        private readonly GroundChecker _spiderGroundChecker;

        public FallingWithoutEnergyState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, ICutSceneService cutSceneService, Spider spider,
            StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(stateMachine, inputService,
            staticDataService, cutSceneService, spider,
            stateMachineData, legs, flower, energySystem)
        {
            _spiderGroundChecker = spider.GroundChecker;
        }

        public override void Enter()
        {
            base.Enter();

            Data.GlobalY = Spider.transform.position.y;
            Data.AirbornSpeed = SpiderStaticData.FallWithoutEnergySpeed;
            Data.IsFallingDownWithoutEnergyState = true;

            EnergyBarUI.ShowHologram();

            SetCrossLegs();
        }


        public override void Exit()
        {
            base.Exit();

            Spider.Stickers.PlaySticker(StickerEnum.FallingDown);
            Data.IsFallingDownWithoutEnergyState = false;

            SetUncrossLegs();
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