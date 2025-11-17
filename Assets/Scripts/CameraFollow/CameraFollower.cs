using System;
using Infastructure.Common.StableWorlUpManagement;
using Infastructure.Services.CutScene;
using Infastructure.Services.Defeat;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
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
        private ICutSceneService _cutSceneService;
        private IDefeatWindowService _defeatWindowService;
        private IWindowService _windowService;

        private Transform _target;
        private Vector3 _velocity;

        private bool _isMouseRotating;
        private float _mouseSensitivity;
        private CinemachineInputAxisController _axisController;
        private CameraSystem _cameraSystem;

        private float _yRotation;
        private float _xRotation;
        private float _cameraRotationSpeedX;
        private float _cameraRotationSpeedY;

        private float _cameraRotationSpeed;
        private JoystickInputSource _joystickInputSource;

        private float _defaultY;
        private Quaternion _orbitStartRotation;


        [Inject]
        public void Construct(
            IInputService inputService,
            IStaticDataService staticDataService,
            IStableWorldUp stableWorldUp,
            ICutSceneService cutSceneService,
            IDefeatWindowService defeatWindowService,
            IWindowService windowService)
        {
            _windowService = windowService;
            _defeatWindowService = defeatWindowService;
            _cutSceneService = cutSceneService;
            _stableWorldUp = stableWorldUp;
            _staticDataService = staticDataService;
            _inputService = inputService;
        }

        private void Awake() =>
            _mouseSensitivity = 2;

        public void Initialize(CameraSystem cameraSystem) =>
            _cameraSystem = cameraSystem;

        public void SetTarget(Transform spiderTransform) =>
            _target = spiderTransform;

        private void Start()
        {
            _windowService.OnWindowOpened += ReleaseInput;
            _defaultY = _cameraSystem.ThirdPersonFollow.ShoulderOffset.y;

            _joystickInputSource = _inputService.GetInputSource<JoystickInputSource>();
        }

        private void OnDestroy() =>
            _windowService.OnWindowOpened -= ReleaseInput;

        private void FixedUpdate()
        {
            if (_target == null || _defeatWindowService.IsDefeated)
                return;

            MoveToTarget();

            if (!_isMouseRotating)
                RotateToTarget();
        }

        private void Update()
        {
            if (_target == null || _cutSceneService.IsActive || _defeatWindowService.IsDefeated)
                return;

            if (!_isMouseRotating)
                HandleScrollWheel();

            HandleMouse();
            WorldUpRotate();
        }


        private void WorldUpRotate() =>
            _stableWorldUp.Rotate(_target.rotation);


        private void HandleScrollWheel()
        {
            float scrollInput = _inputService.ScrollWheelAxis;

            float maxLenght;
            if (_joystickInputSource == null)
                maxLenght = 7;
            else
                maxLenght = _joystickInputSource.IsGamepadActiveNow() ? 4 : 7;


            if (scrollInput != 0f)
            {
                _mouseSensitivity -= scrollInput * SpiderStaticData.ScrollSensitivity;
                _mouseSensitivity = Mathf.Clamp(_mouseSensitivity, 2, maxLenght);
            }

            float smoothSensitivityY = Mathf.Lerp(_cameraSystem.ThirdPersonFollow.ShoulderOffset.y, _mouseSensitivity,
                Time.deltaTime * 5);

            _cameraSystem.ThirdPersonFollow.ShoulderOffset = new Vector3(
                _cameraSystem.ThirdPersonFollow.ShoulderOffset.x,
                smoothSensitivityY,
                _cameraSystem.ThirdPersonFollow.ShoulderOffset.z);
        }

        private void HandleMouse()
        {
            if (_inputService.CenterMousePressed)
                StartInput();

            if (_inputService.CenterMouseUp)
                ReleaseInput();


            if (_isMouseRotating)
            {
                float mouseX = _inputService.MouseXAxis;
                float mouseY = _inputService.MouseYAxis;

                _xRotation += mouseX * SpiderStaticData.MouseRotationSpeedX;
                _yRotation -= mouseY * SpiderStaticData.MouseRotationSpeedY * Time.deltaTime;

                Vector3 up = _target.transform.up;
                Quaternion yaw = Quaternion.AngleAxis(_xRotation, up);

                Quaternion targetRot = yaw * _orbitStartRotation;

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    Time.deltaTime * _cameraRotationSpeed
                );
            }

            Vector2 rotationVector = new Vector2(0, _yRotation);

            _cameraSystem.ThirdPersonFollow.ShoulderOffset = new Vector3(
                _cameraSystem.ThirdPersonFollow.ShoulderOffset.x,
                Mathf.Lerp(_cameraSystem.ThirdPersonFollow.ShoulderOffset.y, rotationVector.y,
                    _cameraRotationSpeed * Time.deltaTime),
                _cameraSystem.ThirdPersonFollow.ShoulderOffset.z);
        }

        private void ReleaseInput()
        {
            _cameraRotationSpeed = SpiderStaticData.CameraRotationSpeed / 3;

            _yRotation = _defaultY;
            _xRotation = 0;

            _isMouseRotating = false;
        }

        private void StartInput()
        {
            _isMouseRotating = true;

            _cameraRotationSpeed = SpiderStaticData.CameraRotationSpeed;

            _yRotation = _defaultY;
            _xRotation = 0f;

            _orbitStartRotation = transform.rotation;
        }


        private void MoveToTarget()
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                _target.position,
                ref _velocity,
                SpiderStaticData.SmoothTime
            );
        }

        private void RotateToTarget()
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                _target.rotation,
                Time.fixedDeltaTime * SpiderStaticData.CameraRotationSpeed
            );
        }
    }
}