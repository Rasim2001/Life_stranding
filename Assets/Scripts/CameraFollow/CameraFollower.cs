using System;
using Infastructure.Common.StableWorlUpManagement;
using Infastructure.Services.CursorVisible;
using Infastructure.Services.CutScene;
using Infastructure.Services.Defeat;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerInput.InputSourceRealization;
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
        private const float MaxUpDriftAngle = 30;
        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private IInputService _inputService;
        private IStaticDataService _staticDataService;
        private IStableWorldUp _stableWorldUp;
        private ICutSceneService _cutSceneService;
        private IDefeatWindowService _defeatWindowService;
        private IWindowService _windowService;
        private ICursorVisibleService _cursorVisibleService;

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

        private float _maxRotationY = 3.0f;

        [Inject]
        public void Construct(
            IInputService inputService,
            IStaticDataService staticDataService,
            IStableWorldUp stableWorldUp,
            ICutSceneService cutSceneService,
            IDefeatWindowService defeatWindowService,
            IWindowService windowService,
            ICursorVisibleService cursorVisibleService)
        {
            _cursorVisibleService = cursorVisibleService;
            _windowService = windowService;
            _defeatWindowService = defeatWindowService;
            _cutSceneService = cutSceneService;
            _stableWorldUp = stableWorldUp;
            _staticDataService = staticDataService;
            _inputService = inputService;
        }

        private void Awake()
        {
            _mouseSensitivity = 2;
        }

        public void Initialize(CameraSystem cameraSystem) =>
            _cameraSystem = cameraSystem;

        public void SetTarget(Transform spiderTransform) =>
            _target = spiderTransform;

        private void Start()
        {
            _defaultY = _cameraSystem.ThirdPersonFollow.ShoulderOffset.y;
            _cameraRotationSpeed = SpiderStaticData.CameraRotationSpeed;

            _windowService.OnWindowOpened += ReleaseInput;

            _inputService.OnJoystickEnableHappend += JoystickEnabled;
            _inputService.OnJoystickDisableHappend += JoystickDisabled;

            _cursorVisibleService.OnHideCursorHappened += StartInput;
        }

        private void OnDestroy()
        {
            _windowService.OnWindowOpened -= ReleaseInput;

            _inputService.OnJoystickDisableHappend -= JoystickDisabled;
            _inputService.OnJoystickEnableHappend -= JoystickEnabled;

            _cursorVisibleService.OnHideCursorHappened -= StartInput;
        }

        private void Update()
        {
            if (_target == null || _cutSceneService.IsActive || _defeatWindowService.IsDefeated)
                return;

            float upAngle = Vector3.Angle(transform.up, _stableWorldUp.StableWorldUpTransform.up);

            if (upAngle > MaxUpDriftAngle)
                RealignOrbitToWorldUp();

            WorldUpRotate();
            HandleMouse();
        }

        private void FixedUpdate()
        {
            if (_target == null || _defeatWindowService.IsDefeated)
                return;

            MoveToTarget();
        }

        private void LateUpdate()
        {
            if (_isMouseRotating)
                DefaultMoveCamera();
            else
                ClimbMoveCamera();
        }

        private void JoystickDisabled() =>
            _joystickInputSource = null;

        private void JoystickEnabled(IInputSource obj) =>
            _joystickInputSource = (JoystickInputSource)obj;


        private void ClimbMoveCamera()
        {
            float spiderPitch = Mathf.Abs(Mathf.DeltaAngle(0f, _target.transform.eulerAngles.z));
            float targetY;

            if (spiderPitch > 0f)
            {
                float t = Mathf.InverseLerp(0f, 45, spiderPitch);
                t = Mathf.Clamp01(t);

                targetY = Mathf.Lerp(_defaultY, 5, t);
            }
            else
                targetY = _defaultY;

            _yRotation = Mathf.Lerp(
                _yRotation,
                targetY,
                _cameraRotationSpeed * Time.deltaTime);


            float yLerp = Mathf.Lerp(
                _cameraSystem.ThirdPersonFollow.ShoulderOffset.y,
                _yRotation,
                _cameraRotationSpeed * Time.deltaTime);

            _cameraSystem.ThirdPersonFollow.ShoulderOffset = new Vector3(
                _cameraSystem.ThirdPersonFollow.ShoulderOffset.x,
                yLerp,
                _cameraSystem.ThirdPersonFollow.ShoulderOffset.z);
        }

        private void DefaultMoveCamera()
        {
            float mouseX = _inputService.MouseXAxis;
            float mouseY = _inputService.MouseYAxis;

            _xRotation += mouseX * SpiderStaticData.MouseRotationSpeedX;
            _yRotation -= mouseY * SpiderStaticData.MouseRotationSpeedY * Time.deltaTime;

            Vector3 up = _stableWorldUp.StableWorldUpTransform.up;
            Quaternion yaw = Quaternion.AngleAxis(_xRotation, up);

            Quaternion targetRot = yaw * _orbitStartRotation;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * _cameraRotationSpeed
            );

            _yRotation = Mathf.Clamp(_yRotation, 0, _maxRotationY);
            float yLerp = Mathf.Lerp(_cameraSystem.ThirdPersonFollow.ShoulderOffset.y, _yRotation,
                _cameraRotationSpeed * Time.deltaTime);

            _cameraSystem.ThirdPersonFollow.ShoulderOffset = new Vector3(
                _cameraSystem.ThirdPersonFollow.ShoulderOffset.x, yLerp,
                _cameraSystem.ThirdPersonFollow.ShoulderOffset.z);
        }


        private void HandleMouse()
        {
            if (_inputService.LeftMousePressed)
                ReleaseInput();

            if (_inputService.LeftMouseUp)
                StartInput();
        }

        private void WorldUpRotate() =>
            _stableWorldUp.Rotate(_target.rotation);


        /*private void HandleScrollWheel()
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
        }*/

        private void ReleaseInput()
        {
            _xRotation = 0;

            _isMouseRotating = false;
        }

        private void StartInput()
        {
            _isMouseRotating = true;
            _xRotation = 0f;

            Vector3 worldUp = _stableWorldUp.StableWorldUpTransform.up;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, worldUp);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.Cross(worldUp, transform.right);

            forward.Normalize();
            _orbitStartRotation = Quaternion.LookRotation(forward, worldUp);
            transform.rotation = _orbitStartRotation;
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

        private void RealignOrbitToWorldUp()
        {
            Vector3 worldUp = _stableWorldUp.StableWorldUpTransform.up;

            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, worldUp);
            if (forward.sqrMagnitude < Mathf.Epsilon)
                forward = Vector3.Cross(worldUp, transform.right);

            forward.Normalize();

            Quaternion aligned = Quaternion.LookRotation(forward, worldUp);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                aligned,
                Time.deltaTime * 5
            );

            _xRotation = 0f;
            _yRotation = _cameraSystem.ThirdPersonFollow.ShoulderOffset.y;
            _orbitStartRotation = aligned;
        }
    }
}