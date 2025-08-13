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

            foreach (LegDataStruct legData in Legs)
                legData.Raycast.RotateFallingLegs();
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

            /*if (_spiderGroundChecker.IsTouchingGround && _spiderGroundChecker.IsTouchesWithLegs == false)
                Spider.transform.localEulerAngles = Vector3.zero;*/

            if (_spiderGroundChecker.IsTouchesWithLegs || _spiderGroundChecker.IsTouchingGround)
                StandUpAsync().Forget();
        }

        private async UniTask StandUpAsync()
        {
            Data.YVelocity = 0;
            Data.IsStandingUpAfterFalling = true;

            foreach (LegDataStruct legData in Legs)
            {
                legData.Leg.SetAcceleration(0.5f);
                legData.Raycast.SetDefaultRotationLegs();
                legData.Raycast.SetGroundState();
            }

            if (IsInputZero())
                StateMachine.SwitchState<IdlingState>();
            else
                StateMachine.SwitchState<RunningState>();

            await UniTask.Delay(TimeSpan.FromSeconds(2f));

            foreach (LegDataStruct legData in Legs)
                legData.Leg.SetDefaultSpeed();

            Data.IsStandingUpAfterFalling = false;
        }
    }
}