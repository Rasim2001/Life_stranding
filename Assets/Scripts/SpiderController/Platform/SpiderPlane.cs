using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerInput.InputSourceRealization;
using Infastructure.Services.Window;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using SpiderController.StateMachine;
using SpiderController.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace SpiderController.Platform
{
    public class SpiderPlane
    {
        private readonly IInputService _inputService;
        private readonly IAbilityService _abilityService;
        private readonly IStaticDataService _staticDataService;
        private readonly IWindowService _windowService;
        private readonly ICameraProviderService _cameraProviderService;
        private readonly StateMachineData _stateMachineData;
        private readonly PressedMouseButtonIndicatorUI _pressedMouseButtonIndicatorUI;
        private readonly Transform _rotationPlaneTransform;
        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private Vector2 _mouseInput;
        private Vector2 _initialMousePosition;
        private bool _isMouseHold;

        private Quaternion _targetLocalRotationInFallingDownState;

        private JoystickInputSource _joystickInputSource;

        private bool _joystickInputActive;
        private float _waitTimeJoystick;
        private Transform _cameraTransform;


        public SpiderPlane(
            PressedMouseButtonIndicatorUI pressedMouseButtonIndicatorUI,
            Transform rotationPlaneTransform,
            IInputService inputService,
            IAbilityService abilityService,
            IStaticDataService staticDataService,
            IWindowService windowService,
            ICameraProviderService cameraProviderService,
            StateMachineData stateMachineData)
        {
            _windowService = windowService;
            _cameraProviderService = cameraProviderService;
            _inputService = inputService;
            _abilityService = abilityService;
            _staticDataService = staticDataService;
            _pressedMouseButtonIndicatorUI = pressedMouseButtonIndicatorUI;
            _rotationPlaneTransform = rotationPlaneTransform;
            _stateMachineData = stateMachineData;
        }

        public void Initialize()
        {
            _cameraTransform = _cameraProviderService.CameraTransform;

            _windowService.OnWindowOpened += ReleaseInput;

            _inputService.OnJoystickEnableHappend += EnableJoystick;
            _stateMachineData.OnFallingDownStateChanged += OnFallingDownStateEnter;
        }

        public void Destroy()
        {
            _windowService.OnWindowOpened -= ReleaseInput;

            _inputService.OnJoystickEnableHappend -= EnableJoystick;
            _stateMachineData.OnFallingDownStateChanged -= OnFallingDownStateEnter;
        }

        public void Update()
        {
            if (_stateMachineData.IsFallingDownWithoutEnergyState ||
                !_abilityService.IsExploredAbility(ProductType.Flower))
                return;

            if (_inputService.LeftMousePressed)
                StartInput();
            else if (_inputService.LeftMouseUp)
                ReleaseInput();

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

        private void ReleaseInput()
        {
            _pressedMouseButtonIndicatorUI.Hide();
            _isMouseHold = false;
            _mouseInput = Vector2.zero;
        }

        private void StartInput()
        {
            _pressedMouseButtonIndicatorUI.Show();
            _isMouseHold = true;

            Mouse.current.WarpCursorPosition(new Vector2(Screen.width / 2, Screen.height / 2));

            _initialMousePosition = new Vector2(Screen.width / 2, Screen.height / 2);
            _waitTimeJoystick = 0;
        }

        private void EnableJoystick(IInputSource obj) =>
            _joystickInputSource = (JoystickInputSource)obj;

        private void OnFallingDownStateEnter(bool isTrue)
        {
            if (!isTrue)
                return;

            int randomSign = Random.value < 0.5f ? -1 : 1;

            float randomAngleX = Random.Range(30, 40f) * randomSign;
            float randomAngleY = Random.Range(30, 40f) * randomSign;
            float randomAngleZ = Random.Range(30, 40f) * randomSign;

            _targetLocalRotationInFallingDownState = Quaternion.Euler(randomAngleX, randomAngleY, randomAngleZ);
        }

        public void FixedUpdate()
        {
            if (_stateMachineData.IsFallingDownWithoutEnergyState)
                RotateTo(_targetLocalRotationInFallingDownState);
            else
                ApplyRotation();
        }

        private void HandleMousePosition()
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            _mouseInput.x = (mousePos.x - _initialMousePosition.x) / (screenSize.x / 2);
            _mouseInput.y = (mousePos.y - _initialMousePosition.y) / (screenSize.y / 2);

            _mouseInput = Vector2.ClampMagnitude(_mouseInput, 1f);

            _mouseInput.x = -_mouseInput.x;
            _mouseInput.y = -_mouseInput.y;

            _mouseInput *= SpiderStaticData.PlaneSensitivity;

            _mouseInput = ConvertInputFromCameraToSpiderSpace(_mouseInput);
        }

        private void HandleJoystickPosition()
        {
            _mouseInput = new Vector2(-_inputService.MouseXAxis, -_inputService.MouseYAxis) *
                          SpiderStaticData.PlaneSensitivity;

            if (_mouseInput.sqrMagnitude > Mathf.Epsilon && _joystickInputActive == false)
            {
                _pressedMouseButtonIndicatorUI.Show();
                _joystickInputActive = true;
            }
            else if (_mouseInput.sqrMagnitude < Mathf.Epsilon && _joystickInputActive)
            {
                _pressedMouseButtonIndicatorUI.Hide();
                _joystickInputActive = false;
            }
        }

        private void ApplyRotation()
        {
            float targetAngleX = Mathf.Clamp(-_mouseInput.y * SpiderStaticData.MaxAngle, -SpiderStaticData.MaxAngle,
                SpiderStaticData.MaxAngle);
            float targetAngleZ = Mathf.Clamp(_mouseInput.x * SpiderStaticData.MaxAngle, -SpiderStaticData.MaxAngle,
                SpiderStaticData.MaxAngle);

            Quaternion targetLocalRotation = Quaternion.Euler(targetAngleX, 0f, targetAngleZ);

            RotateTo(targetLocalRotation);
        }


        private void RotateTo(Quaternion targetLocalRotation)
        {
            _rotationPlaneTransform.localRotation = Quaternion.Slerp(
                _rotationPlaneTransform.localRotation,
                targetLocalRotation,
                Time.fixedDeltaTime * SpiderStaticData.PlaneRotationSpeed);
        }

        private Vector2 ConvertInputFromCameraToSpiderSpace(Vector2 input)
        {
            if (_cameraTransform == null || _rotationPlaneTransform == null || _rotationPlaneTransform.parent == null)
                return input;

            Vector3 spiderForward = _rotationPlaneTransform.parent.forward;
            Vector3 cameraForward = _cameraTransform.forward;

            spiderForward.y = 0f;
            cameraForward.y = 0f;

            if (spiderForward.sqrMagnitude < 0.001f || cameraForward.sqrMagnitude < 0.001f)
                return input;

            spiderForward.Normalize();
            cameraForward.Normalize();

            float signedAngle = Vector3.SignedAngle(cameraForward, spiderForward, Vector3.up);
            float rad = signedAngle * Mathf.Deg2Rad;

            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            Vector2 result;
            result.x = input.x * cos - input.y * sin;
            result.y = input.x * sin + input.y * cos;

            return result;
        }
    }
}