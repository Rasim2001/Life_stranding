using System;
using Infastructure.Services.PlayerInput.InputSourceRealization;
using UnityEngine;
using Zenject;

namespace Infastructure.Services.PlayerInput
{
    public class InputService : IInputService, IDisposable
    {
        public event Action<IInputSource> OnJoystickEnableHappend;

        private IInputSource _inputSource;
        private IInputSource _joystickInputSource;

        public void Initialize()
        {
            _inputSource = new CutSceneInputSource();
            _inputSource.Enable();
        }

        public void Dispose()
        {
            _joystickInputSource.Disable();
            _inputSource.Disable();

            _joystickInputSource = null;
            _inputSource = null;
        }

        public void SetInputSource(IInputSource inputSource)
        {
            if (_joystickInputSource == null)
            {
                _joystickInputSource = new JoystickInputSource();
                _joystickInputSource.Enable();

                OnJoystickEnableHappend?.Invoke(_joystickInputSource);
            }

            _inputSource?.Disable();
            _inputSource = inputSource;
            _inputSource.Enable();
        }

        public T GetInputSource<T>()
        {
            if (_inputSource is T)
                return (T)_inputSource;

            return (T)_joystickInputSource;
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

        public bool RightMousePressed =>
            _inputSource.RightMousePressed || (_joystickInputSource?.RightMousePressed ?? false);

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

        public void Tick()
        {
            throw new NotImplementedException();
        }
    }
}