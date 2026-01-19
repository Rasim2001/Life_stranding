using System;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput.InputSourceRealization;
using Infastructure.Services.StartGame;
using Infastructure.Services.Window;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Infastructure.Services.PlayerInput
{
    public class InputService : IInputService, IDisposable
    {
        public event Action<IInputSource> OnJoystickEnableHappend;
        public event Action OnJoystickDisableHappend;

        private readonly IWindowService _windowService;
        private readonly ICutSceneService _cutSceneService;

        private IInputSource _inputSource;
        private IInputSource _joystickInputSource;

        private bool _leftMouseUpOnce;
        private bool _rightMouseUpOnce;
        private bool _centerMouseUpOnce;
        private bool _isLeftShiftUpOnce;
        private bool _ctrlUpOnce;
        private bool _jumpUpOnce;

        private bool _isConnected;
        private IStartGameReceiver _startGameReceiver;

        public InputService(IWindowService windowService, IStartGameReceiver startGameReceiver,
            ICutSceneService cutSceneService)
        {
            _startGameReceiver = startGameReceiver;
            _cutSceneService = cutSceneService;
            _windowService = windowService;
        }


        public Vector3 InputVector
        {
            get
            {
                Vector3 keyboardInput = _inputSource.InputVector;

                return keyboardInput.sqrMagnitude > Mathf.Epsilon
                    ? _inputSource.InputVector
                    : _joystickInputSource?.InputVector ?? Vector3.zero;
            }
        }
        
        public bool PauseButtonPressed =>
            _inputSource.PauseButtonPressed || (_joystickInputSource?.PauseButtonPressed ?? false);

        public bool TabPressed => _inputSource.TabPressed || (_joystickInputSource?.TabPressed ?? false);

        public bool LeftMousePressed => _inputSource.LeftMousePressed;

        public bool LeftMouseUp => _inputSource.LeftMouseUp;

        public bool RightMousePressed
        {
            get
            {
                if (_leftMouseUpOnce)
                {
                    _leftMouseUpOnce = false;
                    return _leftMouseUpOnce;
                }

                return _inputSource.RightMousePressed || (_joystickInputSource?.RightMousePressed ?? false);
            }
        }

        public bool RightMouseUp => _inputSource.RightMouseUp || (_joystickInputSource?.RightMouseUp ?? false);

        public bool CenterMousePressed =>
            _inputSource.CenterMousePressed || (_joystickInputSource?.CenterMousePressed ?? false);

        public bool CenterMouseUp => _inputSource.CenterMouseUp || (_joystickInputSource?.CenterMouseUp ?? false);

        public float ScrollWheelAxis
        {
            get
            {
                float keyboardInput = Mathf.Abs(_inputSource.ScrollWheelAxis);

                return keyboardInput > 0
                    ? _inputSource.ScrollWheelAxis
                    : _joystickInputSource?.ScrollWheelAxis ?? 0;
            }
        }

        public float MouseXAxis
        {
            get
            {
                float keyboardInput = Mathf.Abs(_inputSource.MouseXAxis);

                return keyboardInput > 0
                    ? _inputSource.MouseXAxis
                    : _joystickInputSource?.MouseXAxis ?? 0;
            }
        }

        public float MouseYAxis
        {
            get
            {
                float keyboardInput = Mathf.Abs(_inputSource.MouseYAxis);

                return keyboardInput > 0
                    ? _inputSource.MouseYAxis
                    : _joystickInputSource?.MouseYAxis ?? 0;
            }
        }

        public bool IsLeftShiftPressed =>
            _inputSource.IsLeftShiftPressed || (_joystickInputSource?.IsLeftShiftPressed ?? false);

        public bool IsLeftShiftUp => _inputSource.IsLeftShiftUp || (_joystickInputSource?.IsLeftShiftUp ?? false);

        public bool CtrlPressed => _inputSource.CtrlPressed || (_joystickInputSource?.CtrlPressed ?? false);

        public bool CtrlUp => _inputSource.CtrlUp || (_joystickInputSource?.CtrlUp ?? false);

        public bool JumpPressed => _inputSource.JumpPressed || (_joystickInputSource?.JumpPressed ?? false);

        public bool JumpUp => _inputSource.JumpUp || (_joystickInputSource?.JumpUp ?? false);

        public bool JerkPressed => _inputSource.JerkPressed || (_joystickInputSource?.JerkPressed ?? false);

        public bool PickupPressed => _inputSource.PickupPressed || (_joystickInputSource?.PickupPressed ?? false);

        public void Initialize()
        {
            _windowService.OnWindowOpened += WindowOpenedUI;
            _startGameReceiver.OnStartGameHappened += SetPlayerInputSource;
            _cutSceneService.OnCutsceneActiveChanged += CutSceneActiveChanged;


            _inputSource = new CutSceneInputSource();
            _inputSource.Enable();

            InputSystem.onDeviceChange += DeviceChanged;
        }


        public void Dispose()
        {
            _windowService.OnWindowOpened -= WindowOpenedUI;
            _startGameReceiver.OnStartGameHappened -= SetPlayerInputSource;
            _cutSceneService.OnCutsceneActiveChanged -= CutSceneActiveChanged;

            _joystickInputSource?.Disable();
            _inputSource?.Disable();

            _joystickInputSource = null;
            _inputSource = null;

            InputSystem.onDeviceChange -= DeviceChanged;
        }

        public bool AnyActionPressed =>
            _inputSource.AnyKeyPressed();

        public T GetInputSource<T>() =>
            (T)_inputSource;

        public bool IsActiveSource<T>() =>
            _inputSource is T;

        private void SetPlayerInputSource() =>
            SetInputSource(new PlayerInputSource());

        private void CutSceneActiveChanged(bool isActive)
        {
            if (isActive)
                SetInputSource(new CutSceneInputSource());
            else
                SetInputSource(new PlayerInputSource());
        }


        private void SetInputSource(IInputSource inputSource)
        {
            _inputSource?.Disable();

            if (Gamepad.current != null)
            {
                _inputSource = new JoystickInputSource();
                OnJoystickEnableHappend?.Invoke(_inputSource);
            }
            else
            {
                _inputSource = inputSource;
                OnJoystickDisableHappend?.Invoke();
            }

            _inputSource.Enable();
        }


        private void DeviceChanged(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Reconnected:
                    SetGamepadState(true);
                    break;

                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                    SetGamepadState(false);
                    break;
            }
        }

        private void SetGamepadState(bool isConnected)
        {
            Debug.Log($"{isConnected}");

            if (_isConnected == isConnected)
                return;

            if (isConnected)
                SetInputSource(new JoystickInputSource());
            else
                SetInputSource(new PlayerInputSource());

            _isConnected = isConnected;
        }

        private void WindowOpenedUI()
        {
            _leftMouseUpOnce = true;
            _rightMouseUpOnce = true;
            _centerMouseUpOnce = true;
            _isLeftShiftUpOnce = true;
            _isLeftShiftUpOnce = true;
            _ctrlUpOnce = true;
            _jumpUpOnce = true;
        }
    }
}