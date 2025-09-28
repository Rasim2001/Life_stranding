using System;
using Cysharp.Threading.Tasks;
using GameDevBuddies;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Airborn;

namespace SpiderController.StateMachine.States.Ground
{
    public class GroundedState : MovementState
    {
        private readonly GroundChecker _groundChecker;

        protected GroundedState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(stateMachine, inputService,
            staticDataService, spider,
            stateMachineData, legs, flower, energySystem)
        {
            _groundChecker = spider.GroundChecker;
        }

        public override void Enter()
        {
            base.Enter();

            if (IsNotMoveableLayer())
                StateMachine.SwitchState<RecoveryState>();
        }


        public override void Exit()
        {
            base.Exit();

            if (IsNotMoveableLayer() == false)
            {
                Data.LastValidGroundPosition = Spider.Rigidbody.position;
                Data.LastValidGroundRotation = Spider.Rigidbody.rotation;
            }
        }


        public override void Update()
        {
            base.Update();

            if (InputService.TabPressed)
                StartTerrainScan().Forget();

            if (IsNotMoveableLayer())
                return;

            if (_groundChecker.IsTouchesWithLegs == false)
                StateMachine.SwitchState<FallingState>();

            if (InputService.JumpPressed && Data.EnergyFillAmount > 0 && !Data.IsStandingUpAfterFalling)
                StateMachine.SwitchState<JumpingState>();

            if (InputService.JerkPressed && Data.EnergyFillAmount > 0 && !Data.IsStandingUpAfterFalling)
                StateMachine.SwitchState<JerkState>();
        }

        private async UniTask StartTerrainScan()
        {
            Spider.ScannerAnimator.PlayScanAnimation();

            await UniTask.Delay(TimeSpan.FromSeconds(0.4f));

            TerrainScan.Instance.StartTerrainScan();
        }
    }
}