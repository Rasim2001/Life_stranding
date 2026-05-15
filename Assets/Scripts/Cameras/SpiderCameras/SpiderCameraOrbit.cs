using Infastructure.Common.StableWorlUpManagement;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CursorVisible;
using Infastructure.Services.Defeat;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerInput.InputSourceRealization;
using Infastructure.Services.Registries.SpiderRegistry;
using Infastructure.Services.Window;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using SpiderController;
using SpiderController.StateMachine;
using UnityEngine;

namespace Cameras.SpiderCameras
{
    public class SpiderCameraOrbit
    {
        private const float MaxUpDriftAngle = 30f;

        private readonly IInputService _inputService;
        private readonly IStaticDataService _staticDataService;
        private readonly IStableWorldUp _stableWorldUp;
        private readonly IDefeatWindowService _defeatWindowService;
        private readonly IWindowService _windowService;
        private readonly ICursorVisibleService _cursorVisibleService;
        private readonly ICameraProviderService _cameraProviderService;
        private readonly ISpiderRegistryService _spiderRegistryService;
        private readonly ISpiderCamera _spiderCamera;
        private readonly Transform _pivot;

        private Spider Spider => _spiderRegistryService.Spider;
        private StateMachineData Data => _spiderRegistryService.Spider.StateMachineData;

        private JoystickInputSource _joystickInputSource;

        private float _cameraRotationSpeed;
        private float _defaultShoulderY;

        private bool _isMouseRotating;
        private bool _centerMouseHolding;

        private float _yRotation;
        private float _xRotation;
        private float _xRotationAiming;

        private Quaternion _orbitStartRotation;
        private Quaternion _orbitStartRotationAiming;

        private readonly float _maxRotationY = 3.0f;

        public SpiderCameraOrbit(
            IInputService inputService,
            IStaticDataService staticDataService,
            IStableWorldUp stableWorldUp,
            IDefeatWindowService defeatWindowService,
            IWindowService windowService,
            ICursorVisibleService cursorVisibleService,
            ICameraProviderService cameraProviderService,
            ISpiderRegistryService spiderRegistryService,
            ISpiderCamera spiderCamera,
            Transform pivot)
        {
            _inputService = inputService;
            _staticDataService = staticDataService;
            _stableWorldUp = stableWorldUp;
            _defeatWindowService = defeatWindowService;
            _windowService = windowService;
            _cursorVisibleService = cursorVisibleService;
            _cameraProviderService = cameraProviderService;
            _spiderRegistryService = spiderRegistryService;
            _spiderCamera = spiderCamera;
            _pivot = pivot;
        }


        public void Initialize()
        {
            StartInput();

            _defaultShoulderY = _spiderCamera.ShoulderOffset.y;
            _cameraRotationSpeed = _staticDataService.SpiderStaticData.CameraRotationSpeed;

            _windowService.OnWindowOpened += ReleaseInput;
            _inputService.OnJoystickEnableHappend += JoystickEnabled;
            _inputService.OnJoystickDisableHappend += JoystickDisabled;
            _cursorVisibleService.OnHideCursorHappened += StartInput;
        }

        public void Destroy()
        {
            _windowService.OnWindowOpened -= ReleaseInput;
            _inputService.OnJoystickEnableHappend -= JoystickEnabled;
            _inputService.OnJoystickDisableHappend -= JoystickDisabled;
            _cursorVisibleService.OnHideCursorHappened -= StartInput;
        }

        public void Update()
        {
            if (Spider == null || _defeatWindowService.IsDefeated)
                return;

            CameraCalculateHandle();

            if (_isMouseRotating)
            {
                if (_centerMouseHolding)
                    RotateCameraAiming();
                else
                    RotateCamera();
            }

            float upAngle = Vector3.Angle(_pivot.up, _cameraProviderService.CameraTransform.up);
            if (upAngle > MaxUpDriftAngle)
                RealignOrbitToWorldUp();

            HandleMouse();
        }

        public void AlignToSpider()
        {
            Vector3 worldUp = _stableWorldUp.StableWorldUpTransform.up;
            Vector3 forward = Vector3.ProjectOnPlane(Spider.transform.forward, worldUp).normalized;

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.ProjectOnPlane(Spider.transform.right, worldUp).normalized;

            _orbitStartRotation = Quaternion.LookRotation(forward, worldUp);
            _pivot.rotation = _orbitStartRotation;
            _xRotation = 0f;
        }

        private void CameraCalculateHandle()
        {
            if (!_centerMouseHolding)
            {
                if (_isMouseRotating)
                    CalculateMoveCamera();
                else
                    ClimbMoveCamera();
            }
            else
            {
                _xRotationAiming += _inputService.MouseXAxis * _staticDataService.SpiderStaticData.MouseRotationSpeedX;
                ClimbMoveCamera();
            }
        }

        private void RotateCamera()
        {
            Vector3 up = _stableWorldUp.StableWorldUpTransform.up;
            Quaternion targetRot = Quaternion.AngleAxis(_xRotation, up) * _orbitStartRotation;

            _pivot.rotation = Quaternion.Slerp(
                _pivot.rotation,
                targetRot,
                Time.deltaTime * _cameraRotationSpeed);
        }

        private void RotateCameraAiming()
        {
            Vector3 up = _stableWorldUp.StableWorldUpTransform.up;
            Quaternion targetRot = Quaternion.AngleAxis(_xRotationAiming, up) * _orbitStartRotationAiming;

            _pivot.rotation = Quaternion.Slerp(
                _pivot.rotation,
                targetRot,
                Time.deltaTime * _cameraRotationSpeed);
        }

        private void ClimbMoveCamera()
        {
            float spiderPitch = Mathf.Abs(Mathf.DeltaAngle(0f, Spider.transform.eulerAngles.z));
            float targetY = spiderPitch > 0f
                ? Mathf.Lerp(_defaultShoulderY, 5f, Mathf.Clamp01(Mathf.InverseLerp(0f, 45f, spiderPitch)))
                : _defaultShoulderY;

            _yRotation = Mathf.Lerp(_yRotation, targetY, _cameraRotationSpeed * Time.deltaTime);

            float yLerp = Mathf.Lerp(
                _spiderCamera.ShoulderOffset.y,
                _yRotation,
                _cameraRotationSpeed * Time.deltaTime);

            _spiderCamera.ShoulderOffset = new Vector3(
                _spiderCamera.ShoulderOffset.x,
                yLerp,
                _spiderCamera.ShoulderOffset.z);
        }

        private void CalculateMoveCamera()
        {
            SpiderStaticData data = _staticDataService.SpiderStaticData;

            _xRotation += _inputService.MouseXAxis * data.MouseRotationSpeedX;
            _yRotation -= _inputService.MouseYAxis * data.MouseRotationSpeedY * Time.deltaTime;
            _yRotation = Mathf.Clamp(_yRotation, 0f, _maxRotationY);

            float yLerp = Mathf.Lerp(
                _spiderCamera.ShoulderOffset.y,
                _yRotation,
                _cameraRotationSpeed * Time.deltaTime);

            _spiderCamera.ShoulderOffset = new Vector3(
                _spiderCamera.ShoulderOffset.x,
                yLerp,
                _spiderCamera.ShoulderOffset.z);
        }

        private void HandleMouse()
        {
            if (Spider == null || Data.IsGravityGunState)
                return;

            if (_inputService.CenterMousePressed)
            {
                _centerMouseHolding = true;
                _orbitStartRotationAiming = _pivot.rotation;
                _xRotationAiming = 0f;
            }

            if (_inputService.CenterMouseUp)
            {
                StartInput();
                _centerMouseHolding = false;
            }

            if (_inputService.LeftMousePressed)
                ReleaseInput();

            if (_inputService.LeftMouseUp)
                StartInput();
        }

        private void RealignOrbitToWorldUp()
        {
            Vector3 worldUp = _stableWorldUp.StableWorldUpTransform.up;

            Vector3 forward = Vector3.ProjectOnPlane(_pivot.forward, worldUp);
            if (forward.sqrMagnitude < Mathf.Epsilon)
                forward = Vector3.Cross(worldUp, _pivot.right);

            forward.Normalize();

            Quaternion aligned = Quaternion.LookRotation(forward, worldUp);

            _pivot.rotation = Quaternion.Slerp(_pivot.rotation, aligned, Time.deltaTime * 5f);

            _xRotation = 0f;
            _yRotation = _spiderCamera.ShoulderOffset.y;
            _orbitStartRotation = aligned;
        }

        private void ReleaseInput() =>
            _isMouseRotating = false;

        private void StartInput()
        {
            _xRotation = 0;
            _isMouseRotating = true;

            if (_stableWorldUp.StableWorldUpTransform == null)
                return;

            Vector3 worldUp = _stableWorldUp.StableWorldUpTransform.up;
            Vector3 forward = Vector3.ProjectOnPlane(_pivot.forward, worldUp);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.Cross(worldUp, _pivot.right);

            forward.Normalize();

            _orbitStartRotation = Quaternion.LookRotation(forward, worldUp);
            _pivot.rotation = _orbitStartRotation;
        }

        private void JoystickEnabled(IInputSource obj) =>
            _joystickInputSource = (JoystickInputSource)obj;

        private void JoystickDisabled() =>
            _joystickInputSource = null;
    }
}