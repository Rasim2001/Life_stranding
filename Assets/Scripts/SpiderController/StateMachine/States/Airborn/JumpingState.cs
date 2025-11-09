using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Ground;
using SpiderController.TriggerChecker;
using UnityEngine;

namespace SpiderController.StateMachine.States.Airborn
{
    public class JumpingState : AirbornState
    {
        private readonly GroundChecker _spiderGroundChecker;
        private float _offsetJumpingTime;

        public JumpingState(ISpiderStateMachine stateMachine, IInputService inputService,
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

            Spider.WindowService.OnWindowOpened += GoToFallingWithControlState;

            Data.AirbornSpeed = SpiderStaticData.FallSpeed;
            Data.YVelocity = SpiderStaticData.StartYVelocity;

            Spider.MagnetFreezingService.Freeze();
            Spider.ThrusterSystem.Open(true);

            _offsetJumpingTime = 0.5f;

            EnergyBarUI.ShowHologram();
        }


        public override void Exit()
        {
            base.Exit();

            Spider.WindowService.OnWindowOpened -= GoToFallingWithControlState;

            Spider.ThrusterSystem.Open(false);
            Spider.MagnetFreezingService.Unfreeze();
        }

        public override void Update()
        {
            base.Update();

            EnergySystem.SpendEnergy(SpiderStaticData.EnergySpendAirbornSpeed);

            if (InputService.JumpUp)
                GoToFallingWithControlState();

            if (Data.CurrentEnergyFillAmount <= 0)
                StateMachine.SwitchState<FallingWithoutEnergyState>();

            if (Data.YVelocity < 0)
                StateMachine.SwitchState<FallingState>();

            if (_offsetJumpingTime > 0)
                _offsetJumpingTime -= Time.deltaTime;

            if (_offsetJumpingTime <= 0 && _spiderGroundChecker.IsTouchesWithLegs)
            {
                Data.GlobalY = 0;
                Data.YVelocity = 0;

                if (IsInputZero())
                    StateMachine.SwitchState<IdlingState>();
                else
                    StateMachine.SwitchState<RunningState>();
            }
        }

        private void GoToFallingWithControlState() =>
            StateMachine.SwitchState<FallingWithControlState>();
    }
}