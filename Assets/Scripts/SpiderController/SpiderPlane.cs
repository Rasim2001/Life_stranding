using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerInput.InputSourceRealization;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using SpiderController.StateMachine;
using SpiderController.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpiderController
{
    public class SpiderPlane
    {
        private readonly IInputService _inputService;
        private readonly IStaticDataService _staticDataService;
        private readonly StateMachineData _stateMachineData;
        private readonly PressedMouseButtonIndicatorUI _pressedMouseButtonIndicatorUI;
        private readonly Transform _rotationPlaneTransform;
        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private Vector2 _mouseInput;
        private Vector2 _initialMousePosition;
        private bool _isMouseHold;

        private Quaternion _targetLocalRotationInFallingDownState;

        public float _offsetX = -90;
        private JoystickInputSource _joystickInputSource;

        private bool _joystickInputActive;
        private float _waitTimeJoystick;


        public SpiderPlane(
            PressedMouseButtonIndicatorUI pressedMouseButtonIndicatorUI,
            Transform rotationPlaneTransform,
            IInputService inputService,
            IStaticDataService staticDataService,
            StateMachineData stateMachineData)
        {
            _inputService = inputService;
            _staticDataService = staticDataService;
            _pressedMouseButtonIndicatorUI = pressedMouseButtonIndicatorUI;
            _rotationPlaneTransform = rotationPlaneTransform;
            _stateMachineData = stateMachineData;
        }

        public void Initialize()
        {
            _joystickInputSource = _inputService.GetInputSource<JoystickInputSource>();
            _stateMachineData.OnFallingDownStateChanged += OnFallingDownStateEnter;
        }

        public void Destroy() =>
            _stateMachineData.OnFallingDownStateChanged -= OnFallingDownStateEnter;

        public void Update()
        {
            if (_stateMachineData.IsFallingDownWithoutEnergyState)
                return;

            if (_inputService.LeftMousePressed)
            {
                _pressedMouseButtonIndicatorUI.Show();
                _isMouseHold = true;
                _initialMousePosition = Input.mousePosition;
                _waitTimeJoystick = 0;
            }
            else if (_inputService.LeftMouseUp)
            {
                _pressedMouseButtonIndicatorUI.Hide();
                _isMouseHold = false;
                _mouseInput = Vector2.zero;
            }

            if (_isMouseHold)
                HandleMousePosition();
            else
            {
                bool isGamepadActiveNow = _joystickInputSource.IsGamepadActiveNow();

                if (isGamepadActiveNow)
                    _waitTimeJoystick = 2;

                _waitTimeJoystick -= Time.deltaTime;

                if (_joystickInputSource.IsLeftButtonPressed == false && _waitTimeJoystick > 0)
                    HandleJoystickPosition();
            }
        }

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
    }
}