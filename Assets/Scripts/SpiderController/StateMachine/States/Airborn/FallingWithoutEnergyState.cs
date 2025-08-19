using System;
using Cysharp.Threading.Tasks;
using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Ground;
using SpiderController.UI.Stickers;
using UnityEngine;

namespace SpiderController.StateMachine.States.Airborn
{
    public class FallingWithoutEnergyState : AirbornState
    {
        private StickerUI StickerUI => Spider.SpiderUI.StickerUI;

        private readonly GroundChecker _spiderGroundChecker;

        public FallingWithoutEnergyState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower) : base(stateMachine, inputService, staticDataService, spider,
            stateMachineData, legs, flower)
        {
            _spiderGroundChecker = spider.GroundChecker;
        }

        public override void Enter()
        {
            base.Enter();

            Data.AirbornSpeed = SpiderStaticData.FallWithoutEnergySpeed;
            Data.IsFallingDownWithoutEnergyState = true;
        }

        public override void Exit()
        {
            base.Exit();

            StickerUI.PlaySticker(StickerEnum.FallingDown);
            Data.IsFallingDownWithoutEnergyState = false;
        }

        protected override void TryMoveLegs()
        {
            for (int index = 0; index < Legs.Length; index++)
            {
                ref LegDataStruct legData = ref Legs[index];
                legData.Leg.MoveTo(legData.Raycast.AirbornPosition);
            }
        }


        public override void Update()
        {
            base.Update();

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