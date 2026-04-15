using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CutScene;
using Infastructure.Services.Magnet;
using Infastructure.Services.Pause;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Infastructure.StaticData.StaticDataService;
using SpiderController.TriggerChecker;

namespace SpiderController.StateMachine.States.Ground.Aiming
{
    public class AimRunningState : AimingState
    {
        protected AimRunningState(
            ISpiderStateMachine stateMachine,
            SpiderServiceContext serviceContext,
            SpiderStateContext stateContext,
            EnergySystem energySystem) : base(stateMachine, serviceContext, stateContext, energySystem)
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