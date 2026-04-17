using Infastructure.Common.StableWorlUpManagement;
using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerInput.InputSourceRealization;
using Infastructure.Services.Window;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using SpiderController.StateMachine;
using SpiderController.Trajectory;
using SpiderController.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace SpiderController.Platform
{
    public class SpiderPlane
    {
        private readonly SpiderStateContext _stateContext;
        private readonly IInputService _inputService;
        private readonly IAbilityService _abilityService;
        private readonly IStaticDataService _staticDataService;
        private readonly IWindowService _windowService;
        private readonly ICameraProviderService _cameraProviderService;
        private readonly IStableWorldUp _stableWorldUp;
        private TrajectoryRender _trajectoryRender => _stateContext.TrajectoryRender;
        private StateMachineData StateMachineData => _stateContext.Data;
        private PressedMouseButtonIndicatorUI PressedMouseButtonIndicatorUI => _stateContext.SpiderUI.PlaneIndicatorUI;
        private Transform RotationPlaneTransform => _stateContext.RotationPlaneTransform;
        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private Vector2 _mouseInput;
        private Vector2 _initialMousePosition;
        private bool _isMouseHold;

        private Quaternion _targetLocalRotationInFallingDownState;

        private JoystickInputSource _joystickInputSource;

        private bool _joystickInputActive;
        private float _waitTimeJoystick;
        private Transform _cameraTransform;

        private float _returnTimer = 0;
        private float _lastPositionX;
        private bool _centerMouseHolding;

        public SpiderPlane(
            SpiderStateContext stateContext,
            IInputService inputService,
            IAbilityService abilityService,
            IStaticDataService staticDataService,
            IWindowService windowService,
            ICameraProviderService cameraProviderService,
            IStableWorldUp stableWorldUp)
        {
            _stateContext = stateContext;
            _windowService = windowService;
            _cameraProviderService = cameraProviderService;
            _stableWorldUp = stableWorldUp;
            _inputService = inputService;
            _abilityService = abilityService;
            _staticDataService = staticDataService;
        }

        public void Initialize()
        {
            _cameraTransform = _cameraProviderService.CameraTransform;

            _windowService.OnWindowOpened += ReleaseInput;

            _inputService.OnJoystickEnableHappend += EnableJoystick;
            _inputService.OnJoystickDisableHappend += DisableJoystick;

            StateMachineData.OnFallingDownStateChanged += OnFallingDownStateEnter;
            StateMachineData.AimingStateChanged += OnAimingStateChanged;
        }

        public void Destroy()
        {
            _windowService.OnWindowOpened -= ReleaseInput;

            _inputService.OnJoystickEnableHappend -= EnableJoystick;
            _inputService.OnJoystickDisableHappend -= DisableJoystick;

            StateMachineData.OnFallingDownStateChanged -= OnFallingDownStateEnter;
        }

        public void Update()
        {
            if (StateMachineData.IsFallingDownWithoutEnergyState ||
                !_abilityService.IsExploredAbility(ProductType.Flower) ||
                StateMachineData.IsGravityGunState)
                return;

            if (_inputService.CenterMousePressed)
                OnCenterMouseHoldStarted();
            else if (_inputService.CenterMouseUp)
                OnCenterMouseHoldEnded();

            if (_centerMouseHolding)
                _trajectoryRender.FollowTrajectory(RotationPlaneTransform.position, RotationPlaneTransform.up * 10);

            if (!_centerMouseHolding)
            {
                if (_inputService.LeftMousePressed)
                    StartInput();
                else if (_inputService.LeftMouseUp)
                    ReleaseInput();
            }

            if (_isMouseHold)
                HandleMousePosition();
            else if (_joystickInputSource != null)
            {
                bool isGamepadActiveNow = _joystickInputSource.IsGamepadActiveNow();

                if (isGamepadActiveNow)
                    _waitTimeJoystick = 2;

                _waitTimeJoystick -= Time.deltaTime;

                if (_joystickInputSource.IsRotationButtonPressed == false && _waitTimeJoystick > 0)
                    HandleJoystickPosition();
            }
        }

        public void FixedUpdate()
        {
            if (StateMachineData.IsFallingDownWithoutEnergyState)
                RotateWithoutEnergyTo(_targetLocalRotationInFallingDownState);
            else
                ApplyRotation();
        }

        private void OnAimingStateChanged()
        {
            if (StateMachineData.IsInAimingState == false)
                OnCenterMouseHoldEnded();
        }

        private void OnCenterMouseHoldStarted()
        {
            _centerMouseHolding = true;

            _trajectoryRender.Show();
            StartInput();

            _initialMousePosition.y = 1;
        }

        private void OnCenterMouseHoldEnded()
        {
            _centerMouseHolding = false;

            _trajectoryRender.Hide();
            ReleaseInput();
        }

        private void ReleaseInput()
        {
            PressedMouseButtonIndicatorUI.Hide();
            _isMouseHold = false;
            _mouseInput = Vector2.zero;
        }

        private void StartInput()
        {
            PressedMouseButtonIndicatorUI.Show();
            _isMouseHold = true;
            _returnTimer = 0;

            Vector2 center = new Vector2((float)Screen.width / 2, (float)Screen.height / 2);
            Mouse.current.WarpCursorPosition(center);
            _initialMousePosition = center;

            _waitTimeJoystick = 0;
        }

        private void DisableJoystick() =>
            _joystickInputSource = null;

        private void EnableJoystick(IInputSource obj) =>
            _joystickInputSource = (JoystickInputSource)obj;

        private void OnFallingDownStateEnter(bool isTrue)
        {
            if (!isTrue)
                return;

            _returnTimer = 0;

            int randomSign = Random.value < 0.5f ? -1 : 1;

            float randomAngleX = Random.Range(30, 40f) * randomSign;
            float randomAngleY = Random.Range(30, 40f) * randomSign;
            float randomAngleZ = Random.Range(30, 40f) * randomSign;

            _targetLocalRotationInFallingDownState = Quaternion.Euler(randomAngleX, randomAngleY, randomAngleZ);
        }

        private void HandleMousePosition()
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            if (_centerMouseHolding)
                _mouseInput.x = -(mousePos.x - _initialMousePosition.x - _lastPositionX) / (screenSize.x / 2);
            else
                _mouseInput.x = -(mousePos.x - _initialMousePosition.x) / (screenSize.x / 2);

            _mouseInput.y = -(mousePos.y - _initialMousePosition.y) / (screenSize.y / 2);

            _mouseInput = Vector2.ClampMagnitude(_mouseInput, 1f);

            _mouseInput *= SpiderStaticData.PlaneSensitivity;

            _mouseInput = ConvertInputFromCameraToSpiderSpace(_mouseInput);

            _lastPositionX = mousePos.x - _initialMousePosition.x;
        }

        private void HandleJoystickPosition()
        {
            _mouseInput = new Vector2(-_inputService.MouseXAxis, -_inputService.MouseYAxis) *
                          SpiderStaticData.PlaneSensitivity;

            if (_mouseInput.sqrMagnitude > Mathf.Epsilon && _joystickInputActive == false)
            {
                PressedMouseButtonIndicatorUI.Show();
                _joystickInputActive = true;
            }
            else if (_mouseInput.sqrMagnitude < Mathf.Epsilon && _joystickInputActive)
            {
                PressedMouseButtonIndicatorUI.Hide();
                _joystickInputActive = false;
            }
        }

        private void ApplyRotation()
        {
            float targetAngleX = Mathf.Clamp(-_mouseInput.y * SpiderStaticData.MaxAngle, -SpiderStaticData.MaxAngle,
                SpiderStaticData.MaxAngle);
            float targetAngleZ = Mathf.Clamp(_mouseInput.x * SpiderStaticData.MaxAngle, -SpiderStaticData.MaxAngle,
                SpiderStaticData.MaxAngle);

            Vector3 targetLocalEulerAngles = new Vector3(targetAngleX, 0, targetAngleZ);
            Quaternion targetLocalRotation = Quaternion.Euler(targetLocalEulerAngles);

            RotateTo(targetLocalRotation);
        }


        private void RotateTo(Quaternion targetLocalRotation)
        {
            float dt = Time.fixedDeltaTime;
            float t = 1f - Mathf.Exp(-SpiderStaticData.PlaneRotationSpeed * dt);

            /*if (!_isMouseHold)
            {
                _returnTimer = Mathf.Clamp01(_returnTimer + Time.fixedDeltaTime);

                float curveT = SpiderStaticData.PlaneReturnCurve.Evaluate(_returnTimer);

                RotationPlaneTransform.localRotation = Quaternion.Slerp(
                    RotationPlaneTransform.localRotation,
                    targetLocalRotation,
                    curveT);
            }
            else*/
            {
                RotationPlaneTransform.localRotation = Quaternion.Slerp(
                    RotationPlaneTransform.localRotation,
                    targetLocalRotation,
                    t);
            }
        }

        private void RotateWithoutEnergyTo(Quaternion targetLocalRotation)
        {
            RotationPlaneTransform.localRotation = Quaternion.Slerp(
                RotationPlaneTransform.localRotation,
                targetLocalRotation,
                Time.fixedDeltaTime * SpiderStaticData.PlaneRotationSpeed);
        }

        private Vector2 ConvertInputFromCameraToSpiderSpace(Vector2 input)
        {
            Vector3 up = _stableWorldUp.StableWorldUpTransform.up;

            Vector3 spiderForward = Vector3.ProjectOnPlane(RotationPlaneTransform.parent.forward, up);
            Vector3 cameraForward = Vector3.ProjectOnPlane(_cameraTransform.forward, up);

            if (spiderForward.sqrMagnitude < Mathf.Epsilon || cameraForward.sqrMagnitude < Mathf.Epsilon)
                return input;

            spiderForward.Normalize();
            cameraForward.Normalize();

            float signedAngle = Vector3.SignedAngle(cameraForward, spiderForward, up);
            float rad = signedAngle * Mathf.Deg2Rad;

            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(
                input.x * cos - input.y * sin,
                input.x * sin + input.y * cos
            );
        }
    }
}