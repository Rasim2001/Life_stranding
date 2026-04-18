using Infastructure.Services.CameraProvider;
using Infastructure.Services.GravityGun;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.GravityGun;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform;
using UnityEngine;

namespace SpiderController.StateMachine.OverlayStates
{
    public class GravityGunOverlayState : ISpiderState
    {
        private readonly IPlatformObjectsService _platformObjectsService;
        private readonly ISpiderStateMachine _stateMachine;
        private readonly IInputService _inputService;
        private readonly ICameraProviderService _cameraProviderService;
        private readonly IGravityGunDisplayer _displayer;
        private readonly IStaticDataService _staticDataService;
        private readonly SpiderStateContext _stateContext;
        private StateMachineData Data => _stateContext.Data;
        private Transform RotationPlaneTransform => _stateContext.RotationPlaneTransform;
        private GravityGunStaticData GravityGunStaticData => _staticDataService.GravityGunStaticData;
        private Transform CameraTransform => _cameraProviderService.CameraTransform;

        private bool _isHolding;
        private Rigidbody _grabbedRigidbody;


        public GravityGunOverlayState(
            ISpiderStateMachine stateMachine,
            IPlatformObjectsService platformObjectsService,
            IInputService inputService,
            ICameraProviderService cameraProviderService,
            IGravityGunDisplayer displayer,
            IStaticDataService staticDataService,
            SpiderStateContext stateContext
        )
        {
            _platformObjectsService = platformObjectsService;
            _stateMachine = stateMachine;
            _inputService = inputService;
            _cameraProviderService = cameraProviderService;
            _displayer = displayer;
            _staticDataService = staticDataService;
            _stateContext = stateContext;
        }

        public void Enter()
        {
            Data.IsGravityGunState = true;

            _displayer.Show();
        }

        public void Exit()
        {
            Data.IsGravityGunState = false;

            _displayer.Hide();
        }

        public void HandleInput()
        {
            if (_inputService.GravityGunPressed)
                _stateMachine.SwitchState<EmptyOverlayState>();
        }

        public void Update()
        {
            if (_inputService.LeftMousePressed)
            {
                Ray ray = _displayer.GetAimRay(_cameraProviderService.Camera);

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, GravityGunStaticData.GrabTargetLayer))
                {
                    if (hit.collider != null)
                    {
                        _grabbedRigidbody = hit.collider.GetComponent<Rigidbody>();
                        _grabbedRigidbody.isKinematic = false;
                        _grabbedRigidbody.useGravity = false;
                    }
                }
            }

            if (_inputService.LeftMouseUp && _grabbedRigidbody != null)
            {
                _grabbedRigidbody.useGravity = true;
                _grabbedRigidbody = null;
            }
        }

        public void FixedUpdate()
        {
            if (_grabbedRigidbody != null)
            {
                Move();
                TryPickup();
            }
        }

        private void TryPickup()
        {
            if (Vector3.Distance(_grabbedRigidbody.position, RotationPlaneTransform.position) <
                GravityGunStaticData.GrabDistance)
            {
                PickupObjectBase pickupObjectBase = _grabbedRigidbody.GetComponent<PickupObjectBase>();
                if (pickupObjectBase.IsOnPlatform)
                    return;

                _platformObjectsService.Add(pickupObjectBase);

                _grabbedRigidbody = null;
                _stateMachine.SwitchState<EmptyOverlayState>();
            }
        }

        private void Move()
        {
            Vector3 toTarget = GrabPoint - _grabbedRigidbody.position;

            Vector3 force = toTarget * GravityGunStaticData.GrabForce;

            _grabbedRigidbody.AddForce(force, ForceMode.Acceleration);

            _grabbedRigidbody.linearVelocity =
                Vector3.ClampMagnitude(
                    _grabbedRigidbody.linearVelocity,
                    GravityGunStaticData.MaxGrabVelocity
                );
        }

        public void LateUpdate()
        {
        }

        private Vector3 GrabPoint =>
            CameraTransform.position +
            CameraTransform.forward * GravityGunStaticData.GrabDistance;
    }
}