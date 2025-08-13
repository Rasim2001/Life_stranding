using System;
using Infastructure.Common;
using Infastructure.Services.Input;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace CameraFollow
{
    public class CameraFollower : MonoBehaviour
    {
        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private IInputService _inputService;
        private IStaticDataService _staticDataService;
        private IStableWorldUp _stableWorldUp;

        private Transform _target;
        private Vector3 _velocity;

        private bool _isMouseRotating;
        private float _mouseSensitivity;
        private CinemachineInputAxisController _axisController;
        private CameraSystem _cameraSystem;


        [Inject]
        public void Construct(
            IInputService inputService,
            IStaticDataService staticDataService,
            IStableWorldUp stableWorldUp)
        {
            _stableWorldUp = stableWorldUp;
            _staticDataService = staticDataService;
            _inputService = inputService;
        }

        private void Awake() =>
            _mouseSensitivity = 3;

        public void Initialize(CameraSystem cameraSystem) =>
            _cameraSystem = cameraSystem;

        public void SetTarget(Transform spiderTransform) =>
            _target = spiderTransform;

        private void Start() =>
            CinemachineCore.CameraUpdatedEvent.AddListener(UpdateAfterCinemachine);

        private void OnDestroy() =>
            CinemachineCore.CameraUpdatedEvent.RemoveListener(UpdateAfterCinemachine);

        private void WorldUpRotate() =>
            _stableWorldUp.Rotate(_target.rotation);

        private void FixedUpdate()
        {
            if (_target == null)
                return;

            MoveToTarget();
        }

        private void Update()
        {
            if (_target == null)
                return;

            HandleScrollWheel();
        }

        private void UpdateAfterCinemachine(CinemachineBrain _)
        {
            if (_target == null)
                return;

            HandleMouse();
            WorldUpRotate();
        }

        private void HandleScrollWheel()
        {
            float scrollInput = _inputService.ScrollWheelAxis;

            if (scrollInput != 0f)
            {
                _mouseSensitivity -= scrollInput * SpiderStaticData.ScrollSensitivity;
                _mouseSensitivity = Mathf.Clamp(_mouseSensitivity, 2, 7);
            }

            float smoothSensitivityY = Mathf.Lerp(_cameraSystem.OrbitalFollow.TargetOffset.y, _mouseSensitivity,
                Time.deltaTime * 5);

            _cameraSystem.OrbitalFollow.TargetOffset = new Vector3(0, smoothSensitivityY, 0);
        }

        private void HandleMouse()
        {
            if (_inputService.CenterMousePressed)
            {
                _isMouseRotating = true;
                _cameraSystem.CinemachineInputAxisController.enabled = true;
            }


            if (_inputService.CenterMouseUp)
            {
                _isMouseRotating = false;
                _cameraSystem.CinemachineInputAxisController.enabled = false;
            }


            if (_isMouseRotating)
            {
            }
        }


        private void MoveToTarget()
        {
            transform.localPosition = Vector3.SmoothDamp(
                transform.position,
                _target.position,
                ref _velocity,
                SpiderStaticData.SmoothTime
            );
        }
    }
}