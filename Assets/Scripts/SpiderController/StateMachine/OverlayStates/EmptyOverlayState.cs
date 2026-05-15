using Infastructure.Services.PlayerInput;

namespace SpiderController.StateMachine.OverlayStates
{
    public class EmptyOverlayState : ISpiderState
    {
        private readonly ISpiderStateMachine _stateMachine;
        private readonly IInputService _inputService;

        public EmptyOverlayState(ISpiderStateMachine stateMachine, IInputService inputService)
        {
            _stateMachine = stateMachine;
            _inputService = inputService;
        }

        public void Enter()
        {
        }

        public void Exit()
        {
        }

        public void HandleInput()
        {
            if (_inputService.GravityGunPressed)
                _stateMachine.SwitchState<GravityGunOverlayState>();

            if (_inputService.TeleportPressed)
                _stateMachine.SwitchState<TeleportOverlayState>();

            if (_inputService.CenterMousePressed)
                _stateMachine.SwitchState<AimingOverlayState>();
        }

        public void Update()
        {
        }

        public void FixedUpdate()
        {
        }

        public void LateUpdate()
        {
        }
    }
}