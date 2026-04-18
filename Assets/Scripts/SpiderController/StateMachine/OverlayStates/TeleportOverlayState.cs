using Common.Extensions;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Teleports;
using UnityEngine;

namespace SpiderController.StateMachine.OverlayStates
{
    public class TeleportOverlayState : ISpiderState
    {
        private const float MaxRayDistance = 30f;
        private const float MinRayDistance = 10;

        private readonly ISpiderStateMachine _stateMachine;
        private readonly ITeleportService _teleportService;
        private readonly IInputService _inputService;
        private readonly ICameraProviderService _cameraProviderService;
        private readonly ITeleportDisplayer _displayer;
        private readonly SpiderStateContext _stateContext;

        private StateMachineData Data => _stateContext.Data;

        public TeleportOverlayState(
            ISpiderStateMachine stateMachine,
            ITeleportService teleportService,
            IInputService inputService,
            ICameraProviderService cameraProviderService,
            ITeleportDisplayer displayer,
            SpiderStateContext stateContext)
        {
            _stateMachine = stateMachine;
            _teleportService = teleportService;
            _inputService = inputService;
            _cameraProviderService = cameraProviderService;
            _displayer = displayer;
            _stateContext = stateContext;
        }

        public void Enter()
        {
            Data.IsTeleportState = true;

            _displayer.Show();
        }

        public void Exit()
        {
            Data.IsTeleportState = false;

            _displayer.Hide();
        }

        public void HandleInput()
        {
            if (_inputService.TeleportPressed)
                _stateMachine.SwitchState<EmptyOverlayState>();
        }

        public void Update()
        {
            if (!_inputService.LeftMousePressed)
                return;

            Ray ray = _displayer.GetAimRay(_cameraProviderService.Camera);

            if (!Physics.Raycast(ray, out RaycastHit hit, MaxRayDistance, CollisionLayer.Default.AsMask()))
            {
                Vector3 minFallbackPosition = ray.origin + ray.direction * MinRayDistance;
                _teleportService.SpawnNewTeleport(minFallbackPosition);

                return;
            }

            float distance = Vector3.Distance(hit.point, ray.origin);
            if (distance < MinRayDistance)
                return;

            Vector3 offset = (hit.point - ray.origin).normalized * 1.25f;
            Vector3 fallbackPosition = hit.point - offset;

            _teleportService.SpawnNewTeleport(fallbackPosition);

            _stateMachine.SwitchState<EmptyOverlayState>();
        }

        public void FixedUpdate()
        {
        }

        public void LateUpdate()
        {
        }
    }
}