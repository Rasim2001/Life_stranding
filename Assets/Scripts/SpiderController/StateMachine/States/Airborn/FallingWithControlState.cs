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
using UnityEngine;

namespace SpiderController.StateMachine.States.Airborn
{
    public class FallingWithControlState : AirbornState
    {
        private readonly GroundChecker _spiderGroundChecker;

        public FallingWithControlState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, ICutSceneService cutSceneService,
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

            SetCrossLegs();
        }

        public override void Exit()
        {
            base.Exit();

            SetUncrossLegs();
        }


        public override void Update()
        {
            base.Update();

            if (InputService.JumpPressed)
                StateMachine.SwitchState<FallingState>();

            if (_spiderGroundChecker.IsTouchesWithLegs)
            {
                ShakeCamera();

                Data.YVelocity = 0;
                Spider.Stickers.PlaySticker(StickerEnum.FallingDown);

                if (IsInputZero())
                    StateMachine.SwitchState<IdlingState>();
                else
                    StateMachine.SwitchState<RunningState>();
            }
        }
    }
}