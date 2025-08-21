using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Ground;
using SpiderController.UI.Stickers;

namespace SpiderController.StateMachine.States.Airborn
{
    public class FallingWithControlState : AirbornState
    {
        private StickerUI StickerUI => Spider.SpiderUI.StickerUI;

        private readonly GroundChecker _spiderGroundChecker;

        public FallingWithControlState(ISpiderStateMachine stateMachine, IInputService inputService,
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
        }


        public override void Update()
        {
            base.Update();

            if (InputService.JumpPressed)
                StateMachine.SwitchState<FallingState>();

            if (_spiderGroundChecker.IsTouchesWithLegs)
            {
                Data.YVelocity = 0;
                StickerUI.PlaySticker(StickerEnum.FallingDown);

                if (IsInputZero())
                    StateMachine.SwitchState<IdlingState>();
                else
                    StateMachine.SwitchState<RunningState>();
            }
        }
    }
}