using Common.Extensions;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Teleports;
using SpiderController;
using SpiderController.StateMachine;
using SpiderController.StateMachine.OverlayStates;
using UnityEngine;

public class TeleportOverlayState : ISpiderState
{
    private const float MaxRayDistance = 30f;
    private const float MinRayDistance = 10f;

    private static readonly Vector3 PortalHalfExtents = new Vector3(0.5f, 1f, 0.05f);

    private readonly ISpiderStateMachine _stateMachine;
    private readonly ITeleportService _teleportService;
    private readonly IInputService _inputService;
    private readonly ICameraProviderService _cameraProviderService;
    private readonly ITeleportDisplayer _displayer;
    private readonly SpiderStateContext _stateContext;

    private StateMachineData Data => _stateContext.Data;
    private Transform CameraTransform => _cameraProviderService.CameraTransform;

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

        if (TryGetSpawnPoint(out Vector3 position, out Vector3 normal))
            _teleportService.SpawnNewTeleport(position, normal);
        else
        {
            Vector3 fallbackPosition = CameraTransform.position + CameraTransform.forward * MinRayDistance;
            Vector3 fallbackNormal = -CameraTransform.forward;
            _teleportService.SpawnNewTeleport(fallbackPosition, fallbackNormal);
        }
    }

    public void FixedUpdate()
    {
    }

    public void LateUpdate()
    {
    }

    private bool TryGetSpawnPoint(out Vector3 position, out Vector3 normal)
    {
        position = default;
        normal = default;

        Ray ray = _displayer.GetAimRay(_cameraProviderService.Camera);

        if (!Physics.Raycast(ray, out RaycastHit hit, MaxRayDistance, CollisionLayer.Default.AsMask()))
            return false;

        float distance = Vector3.Distance(hit.point, CameraTransform.position);
        if (distance < MinRayDistance)
            return false;

        if (!IsPlacementClear(hit.point, hit.normal))
            return false;

        position = hit.point;
        normal = hit.normal;

        return true;
    }

    private bool IsPlacementClear(Vector3 surfacePoint, Vector3 surfaceNormal)
    {
        Vector3 boxCenter = surfacePoint + surfaceNormal * PortalHalfExtents.z;

        Quaternion boxRotation = Quaternion.LookRotation(-surfaceNormal, Vector3.up);

        bool hasOverlap = Physics.CheckBox(
            boxCenter,
            PortalHalfExtents,
            boxRotation,
            CollisionLayer.Default.AsMask());

        return !hasOverlap;
    }
}