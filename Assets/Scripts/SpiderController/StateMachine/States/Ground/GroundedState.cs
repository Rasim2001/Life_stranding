using System;
using Cysharp.Threading.Tasks;
using GameDevBuddies;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Airborn;
using SpiderController.TriggerChecker;
using UnityEngine;

namespace SpiderController.StateMachine.States.Ground
{
    public class GroundedState : MovementState
    {
        private readonly GroundChecker _groundChecker;

        protected GroundedState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, ICutSceneService cutSceneService, Spider spider,
            StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(stateMachine, inputService,
            staticDataService, cutSceneService, spider,
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

            if (InputService.TabPressed && !Spider.EventSystemSelector.HasFocusUI() &&
                Spider.AbilityService.IsExploredAbility(ProductType.TerrainScanSkillProduct))
                StartTerrainScan().Forget();

            if (IsNotMoveableLayer())
                return;

            if (_groundChecker.IsTouchesWithLegs == false)
                StateMachine.SwitchState<FallingWithControlState>();

            if (InputService.JumpPressed && Data.CurrentEnergyFillAmount > 0 && !Data.IsStandingUpAfterFalling &&
                Spider.AbilityService.IsExploredAbility(ProductType.JumpSkillProduct))
                StateMachine.SwitchState<JumpingState>();

            if (InputService.JerkPressed && Data.CurrentEnergyFillAmount > 0 && !Data.IsStandingUpAfterFalling &&
                Spider.AbilityService.IsExploredAbility(ProductType.JerkSkillProduct))
                StateMachine.SwitchState<JerkState>();
        }

        private async UniTask StartTerrainScan()
        {
            if (Data.TerrainTimer > 0)
                return;

            Data.TerrainTimer = 5f;
            Data.TerrainTimerDefault = Data.TerrainTimer;

            Spider.ScannerAnimator.PlayScanAnimation();

            await UniTask.Delay(TimeSpan.FromSeconds(0.4f));

            TerrainScan.Instance.StartTerrainScan();
        }
    }
}