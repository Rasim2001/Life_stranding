using Infastructure.Services.CameraProvider;
using Infastructure.Services.GravityGun;
using Infastructure.Services.PlayerInput;
using PickupObjects.PickUpOnPlatform;
using UnityEngine;

namespace SpiderController.StateMachine.OverlayStates
{
    public class GravityGunOverlayState : ISpiderState
    {
        private readonly ISpiderStateMachine _stateMachine;
        private readonly IInputService _inputService;
        private readonly ICameraProviderService _cameraProviderService;
        private readonly IGravityGunDisplayer _displayer;
        private readonly StateMachineData _data;
        private readonly Transform _rotationPlaneTransform;
        private readonly LayerMask _grabTargetLayer;

        public float GrabDistance = 2;
        public float GrabForce = 2;
        public float MaxGrabVelocity = 8;

        private Transform CameraTransform => _cameraProviderService.CameraTransform;
        private bool _isHolding;
        private Rigidbody _grabbedRigidbody;


        public GravityGunOverlayState(
            ISpiderStateMachine stateMachine,
            IInputService inputService,
            ICameraProviderService cameraProviderService,
            IGravityGunDisplayer displayer,
            StateMachineData data,
            Transform rotationPlaneTransform,
            LayerMask grabTargetLayer)
        {
            _stateMachine = stateMachine;
            _inputService = inputService;
            _cameraProviderService = cameraProviderService;
            _displayer = displayer;
            _data = data;
            _rotationPlaneTransform = rotationPlaneTransform;
            _grabTargetLayer = grabTargetLayer;
        }

        public void Enter()
        {
            _data.IsInGravityGunState = true;

            _displayer.Show();
        }

        public void Exit()
        {
            _data.IsInGravityGunState = false;

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
                if (Physics.Raycast(CameraTransform.position, CameraTransform.forward, out RaycastHit hit,
                        Mathf.Infinity,
                        _grabTargetLayer))
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
                Vector3 toTarget = GrabPoint - _grabbedRigidbody.position;

                Vector3 force = toTarget * GrabForce;

                _grabbedRigidbody.AddForce(force, ForceMode.Acceleration);

                _grabbedRigidbody.linearVelocity =
                    Vector3.ClampMagnitude(
                        _grabbedRigidbody.linearVelocity,
                        MaxGrabVelocity
                    );

                if (Vector3.Distance(_grabbedRigidbody.position, _rotationPlaneTransform.position) < GrabDistance)
                {
                    PickupObjectBase pickupObjectBase = _grabbedRigidbody.GetComponent<PickupObjectBase>();
                    pickupObjectBase.StopSimulatePhysics();

                    _grabbedRigidbody = null;

                    _stateMachine.SwitchState<EmptyOverlayState>();
                }
            }
        }

        public void LateUpdate()
        {
        }

        private Vector3 GrabPoint =>
            CameraTransform.position +
            CameraTransform.forward * GrabDistance;
    }
}