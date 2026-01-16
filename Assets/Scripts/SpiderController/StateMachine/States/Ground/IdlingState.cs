using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.SpiderMove;

namespace SpiderController.StateMachine.States.Ground
{
    public class IdlingState : GroundedState
    {
        public IdlingState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, ICutSceneService cutSceneService, Spider spider,
            StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(stateMachine, inputService,
            staticDataService, cutSceneService, spider,
            stateMachineData, legs, flower, energySystem)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Data.OnTotalWeightChanged += WeightChanged;
            Data.DistanceFromGround = SpiderStaticData.DistanceFromGround;
            SetSpeed(0);

            EnergyBarUI.PlayFadeHologramEffect();
        }

        public override void Exit()
        {
            base.Exit();

            Data.OnTotalWeightChanged -= WeightChanged;
        }

        private void WeightChanged() =>
            SetSpeed(0);

        public override void Update()
        {
            base.Update();

            if (!Data.IsMouseHolding)
                EnergySystem.RestoreEnergy(SpiderStaticData.EnergyFillSpeed);

            if (SlowdownPressed())
                StateMachine.SwitchState<SlowdownState>();

            if (IsInputZero())
                return;

            StateMachine.SwitchState<RunningState>();
        }
    }
}