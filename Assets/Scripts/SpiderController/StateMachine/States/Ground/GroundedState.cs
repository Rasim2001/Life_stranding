using System;
using Cysharp.Threading.Tasks;
using GameDevBuddies;
using Infastructure.Services.CutScene;
using Infastructure.Services.Pause;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.Window;
using PickupObjects;
using SpiderController.Scanner;
using SpiderController.StateMachine.States.Airborn;
using SpiderController.StateMachine.States.Ground.Aiming;
using SpiderController.TriggerChecker;
using SpiderController.UI;

namespace SpiderController.StateMachine.States.Ground
{
    public class GroundedState : MovementState
    {
        protected EnergyBarUI EnergyBarUI => SpiderUI.EnergyBar;
        protected IPlatformObjectsService PlatformObjectsService => ServiceContext.PlatformObjectsService;

        private IEventSystemSelector SystemSelector => ServiceContext.SystemSelector;
        private IPauseService PauseService => ServiceContext.PauseService;
        private ICutSceneService CutSceneService => ServiceContext.CutSceneService;
        private GroundChecker GroundChecker => StateContext.GroundChecker;

        protected GroundedState(
            ISpiderStateMachine stateMachine,
            SpiderServiceContext serviceContext,
            SpiderStateContext stateContext,
            EnergySystem energySystem) : base(stateMachine, serviceContext, stateContext, energySystem)
        {
        }

        private ScannerAnimator ScannerAnimator => StateContext.ScannerAnimator;


        public override void Enter()
        {
            base.Enter();

            if (IsNotMoveableLayer())
                StateMachine.SwitchState<RecoveryState>();

            Data.YVelocity = 0;
        }


        public override void Exit()
        {
            base.Exit();

            if (IsNotMoveableLayer() == false)
            {
                Data.LastValidGroundPosition = Rigidbody.position;
                Data.LastValidGroundRotation = Rigidbody.rotation;
            }
        }


        public override void Update()
        {
            base.Update();

            if (CanUse())
                StartTerrainScan().Forget();

            if (IsNotMoveableLayer())
                return;


            if (InputService.CenterMousePressed)
            {
                if (IsInputZero())
                    StateMachine.SwitchState<AimIdlingState>();
                else
                    StateMachine.SwitchState<AimRunningState>();
            }

            if (GroundChecker.IsTouchesWithLegs == false)
            {
                Data.IsInAimingState = false;

                StateMachine.SwitchState<FallingWithControlState>();
            }


            if (InputService.JumpPressed && Data.CurrentEnergyFillAmount > 0 && !Data.IsStandingUpAfterFalling &&
                AbilityService.IsExploredAbility(ProductType.JumpSkillProduct))
            {
                Data.IsInAimingState = false;

                StateMachine.SwitchState<JumpingState>();
            }

            if (InputService.JerkPressed && Data.CurrentEnergyFillAmount > 0 && !Data.IsStandingUpAfterFalling &&
                AbilityService.IsExploredAbility(ProductType.JerkSkillProduct))
            {
                Data.IsInAimingState = false;

                StateMachine.SwitchState<JerkState>();
            }
        }

        private bool CanUse()
        {
            return InputService.TabPressed && !SystemSelector.HasFocusUI() &&
                   AbilityService.IsExploredAbility(ProductType.TerrainScanSkillProduct) &&
                   !PauseService.IsPaused && !CutSceneService.IsActive;
        }

        private async UniTask StartTerrainScan()
        {
            if (Data.TerrainTimer > 0)
                return;

            Data.TerrainTimer = 5f;
            Data.TerrainTimerDefault = Data.TerrainTimer;

            ScannerAnimator.PlayScanAnimation();

            await UniTask.Delay(TimeSpan.FromSeconds(0.4f));

            TerrainScan.Instance.StartTerrainScan();
        }
    }
}