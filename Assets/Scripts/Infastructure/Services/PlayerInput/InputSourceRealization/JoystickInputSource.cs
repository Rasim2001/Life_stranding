using UnityEngine;
using UnityEngine.InputSystem;

namespace Infastructure.Services.PlayerInput.InputSourceRealization
{
    public class JoystickInputSource : IInputSource
    {
        public bool IsLeftButtonPressed { get; private set; }

        private GameInput _gameInput;
        private InputAction GameInputMovement => _gameInput.Joystick.Movement;

        private bool _rightPressedThisFrame;
        private bool _rightReleasedThisFrame;

        private bool _jumpPressedThisFrame;
        private bool _jumpReleasedThisFrame;

        private bool _jerkPressedThisFrame;

        private bool _sitdownPressedThisFrame;
        private bool _sitdownReleasedThisFrame;

        private bool _scanPressedThisFrame;
        private bool _pickupPressedThisFrame;

        private bool _centerMousePressedThisFrame;
        private bool _centerMouseReleasedThisFrame;

        private bool _shiftPressedThisFrame;
        private bool _shiftMouseReleasedThisFrame;

        public void Enable()
        {
            _gameInput = new GameInput();
            _gameInput.Enable();

            _gameInput.Joystick.Magnet.started += OnRightDown;
            _gameInput.Joystick.Magnet.canceled += OnRightUp;

            _gameInput.Joystick.Jump.started += OnJumpDown;
            _gameInput.Joystick.Jump.canceled += OnJumpUp;

            _gameInput.Joystick.Jerk.started += OnJerkDown;
            _gameInput.Joystick.Scan.started += OnScanDown;
            _gameInput.Joystick.Pickup.started += OnPickupDown;

            _gameInput.Joystick.Sitdown.started += OnCtrlDown;
            _gameInput.Joystick.Sitdown.canceled += OnCtrlUp;

            _gameInput.Joystick.LookButton.started += OnCenterMouseDown;
            _gameInput.Joystick.LookButton.canceled += OnCenterMouseUp;

            _gameInput.Joystick.ShiftMove.started += OnShiftDown;
            _gameInput.Joystick.ShiftMove.canceled += OnShiftUp;
        }

        public void Disable()
        {
            _gameInput.Joystick.Magnet.started -= OnRightDown;
            _gameInput.Joystick.Magnet.canceled -= OnRightUp;

            _gameInput.Joystick.Jump.started -= OnJumpDown;
            _gameInput.Joystick.Jump.canceled -= OnJumpUp;

            _gameInput.Joystick.Jerk.started -= OnJerkDown;
            _gameInput.Joystick.Scan.started += OnScanDown;

            _gameInput.Joystick.Sitdown.started -= OnCenterMouseDown;
            _gameInput.Joystick.Sitdown.canceled -= OnCtrlUp;

            _gameInput.Joystick.LookButton.started -= OnCenterMouseDown;
            _gameInput.Joystick.LookButton.canceled -= OnCenterMouseUp;

            _gameInput.Disable();
        }

        public bool IsGamepadActiveNow()
        {
            if (_gameInput == null)
                return false;

            Gamepad gp = Gamepad.current;
            if (gp == null)
                return false;

            return gp.leftStick.ReadValue().sqrMagnitude > 0.01f
                   || gp.rightStick.ReadValue().sqrMagnitude > 0.01f
                   || gp.dpad.ReadValue().sqrMagnitude > 0
                   || gp.aButton.wasPressedThisFrame
                   || gp.bButton.wasPressedThisFrame
                   || gp.xButton.wasPressedThisFrame
                   || gp.yButton.wasPressedThisFrame
                   || gp.leftTrigger.wasPressedThisFrame
                   || gp.rightTrigger.wasPressedThisFrame;
        }

        public Vector3 InputVector => new Vector3(GameInputMovement.ReadValue<Vector2>().x, 0,
            GameInputMovement.ReadValue<Vector2>().y);

        public bool LeftMousePressed { get; }


        public bool LeftMouseUp { get; }


        public bool RightMousePressed
        {
            get
            {
                bool v = _rightPressedThisFrame;
                _rightPressedThisFrame = false;
                return v;
            }
        }

        public bool RightMouseUp
        {
            get
            {
                bool v = _rightReleasedThisFrame;
                _rightReleasedThisFrame = false;
                return v;
            }
        }

        public bool CenterMousePressed
        {
            get
            {
                bool v = _centerMousePressedThisFrame;
                _centerMousePressedThisFrame = false;
                return v;
            }
        }

        public bool CenterMouseUp
        {
            get
            {
                bool v = _centerMouseReleasedThisFrame;
                _centerMouseReleasedThisFrame = false;
                return v;
            }
        }

        public float ScrollWheelAxis => _gameInput.Joystick.ScrollCamera.ReadValue<float>();

        public float MouseXAxis => _gameInput.Joystick.PlaneRotation.ReadValue<Vector2>().x;

        public float MouseYAxis => _gameInput.Joystick.PlaneRotation.ReadValue<Vector2>().y;

        public bool IsLeftShiftPressed
        {
            get
            {
                bool v = _shiftPressedThisFrame;
                _shiftPressedThisFrame = false;
                _shiftMouseReleasedThisFrame = false;
                return v;
            }
        }

        public bool IsLeftShiftUp
        {
            get
            {
                bool v = _shiftMouseReleasedThisFrame;
                _shiftMouseReleasedThisFrame = false;
                _shiftPressedThisFrame = false;
                return v;
            }
        }

        public bool CtrlPressed
        {
            get
            {
                bool v = _sitdownPressedThisFrame;
                _sitdownPressedThisFrame = false;
                _sitdownReleasedThisFrame = false;
                return v;
            }
        }

        public bool CtrlUp
        {
            get
            {
                bool v = _sitdownReleasedThisFrame;
                _sitdownReleasedThisFrame = false;
                _sitdownPressedThisFrame = false;
                return v;
            }
        }

        public bool JumpPressed
        {
            get
            {
                bool v = _jumpPressedThisFrame;
                _jumpReleasedThisFrame = false;
                _jumpPressedThisFrame = false;
                return v;
            }
        }

        public bool JumpUp
        {
            get
            {
                bool v = _jumpReleasedThisFrame;
                _jumpPressedThisFrame = false;
                _jumpReleasedThisFrame = false;
                return v;
            }
        }

        public bool JerkPressed
        {
            get
            {
                bool v = _jerkPressedThisFrame;
                _jerkPressedThisFrame = false;
                return v;
            }
        }

        public bool PickupPressed
        {
            get
            {
                bool v = _pickupPressedThisFrame;
                _pickupPressedThisFrame = false;
                return v;
            }
        }

        public bool TabPressed
        {
            get
            {
                bool v = _scanPressedThisFrame;
                _scanPressedThisFrame = false;
                return v;
            }
        }

        private void OnRightDown(InputAction.CallbackContext _) => _rightPressedThisFrame = true;

        private void OnRightUp(InputAction.CallbackContext _) => _rightReleasedThisFrame = true;

        private void OnJumpDown(InputAction.CallbackContext _) => _jumpPressedThisFrame = true;

        private void OnJumpUp(InputAction.CallbackContext _) => _jumpReleasedThisFrame = true;

        private void OnJerkDown(InputAction.CallbackContext obj) => _jerkPressedThisFrame = true;

        private void OnCtrlDown(InputAction.CallbackContext obj) => _sitdownPressedThisFrame = true;

        private void OnCtrlUp(InputAction.CallbackContext _) => _sitdownReleasedThisFrame = true;

        private void OnScanDown(InputAction.CallbackContext _) => _scanPressedThisFrame = true;

        private void OnPickupDown(InputAction.CallbackContext _) => _pickupPressedThisFrame = true;

        private void OnCenterMouseDown(InputAction.CallbackContext obj)
        {
            IsLeftButtonPressed = true;

            _centerMousePressedThisFrame = true;
        }

        private void OnCenterMouseUp(InputAction.CallbackContext _)
        {
            IsLeftButtonPressed = false;

            _centerMouseReleasedThisFrame = true;
        }

        private void OnShiftDown(InputAction.CallbackContext obj) => _shiftPressedThisFrame = true;

        private void OnShiftUp(InputAction.CallbackContext obj) => _shiftMouseReleasedThisFrame = true;
    }
}