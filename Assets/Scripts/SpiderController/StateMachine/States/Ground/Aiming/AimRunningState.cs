using Infastructure.Services.CutScene;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.SpiderMove;

namespace SpiderController.StateMachine.States.Ground.Aiming
{
    public class AimRunningState : AimingState
    {
        public AimRunningState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, ICutSceneService cutSceneService,
            IPlatformObjectsService platformObjectsService, Spider spider,
            StateMachineData stateMachineData, LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(
            stateMachine, inputService, staticDataService, cutSceneService, platformObjectsService, spider,
            stateMachineData, legs, flower,
            energySystem)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Data.DistanceFromGround = SpiderStaticData.DistanceFromGround;

            SetSpeed(SpiderStaticData.Speed);
        }

        public override void Update()
        {
            base.Update();

            if (IsInputZero())
                StateMachine.SwitchState<AimIdlingState>();
        }
    }
}