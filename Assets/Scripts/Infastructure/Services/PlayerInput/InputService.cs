using System;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput.InputSourceRealization;
using UnityEngine;

namespace Infastructure.Services.PlayerInput
{
    public class InputService : IInputService, IDisposable
    {
        private IInputSource _inputSource;
        private IInputSource _joystickInputSource;

        public void Initialize()
        {
            _joystickInputSource = new JoystickInputSource();
            _joystickInputSource.Enable();

            SetInputSource(new CutSceneInputSource());
        }

        public void Dispose()
        {
            _joystickInputSource.Disable();
            _inputSource.Disable();
        }

        public void SetInputSource(IInputSource inputSource)
        {
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
                    : _joystickInputSource.InputVector;
            }
        }

        public bool TabPressed => _inputSource.TabPressed || _joystickInputSource.TabPressed;

        public bool LeftMousePressed => _inputSource.LeftMousePressed;

        public bool LeftMouseUp => _inputSource.LeftMouseUp;

        public bool RightMousePressed => _inputSource.RightMousePressed || _joystickInputSource.RightMousePressed;

        public bool RightMouseUp => _inputSource.RightMouseUp || _joystickInputSource.RightMouseUp;

        public bool CenterMousePressed => _inputSource.CenterMousePressed || _joystickInputSource.CenterMousePressed;

        public bool CenterMouseUp => _inputSource.CenterMouseUp || _joystickInputSource.CenterMouseUp;

        public float ScrollWheelAxis
        {
            get
            {
                float keyboardInput = Mathf.Abs(_inputSource.ScrollWheelAxis);

                return keyboardInput > 0
                    ? _inputSource.ScrollWheelAxis
                    : _joystickInputSource.ScrollWheelAxis;
            }
        }

        public float MouseXAxis
        {
            get
            {
                float keyboardInput = Mathf.Abs(_inputSource.MouseXAxis);

                return keyboardInput > 0
                    ? _inputSource.MouseXAxis
                    : _joystickInputSource.MouseXAxis;
            }
        }

        public float MouseYAxis
        {
            get
            {
                float keyboardInput = Mathf.Abs(_inputSource.MouseYAxis);

                return keyboardInput > 0
                    ? _inputSource.MouseYAxis
                    : _joystickInputSource.MouseYAxis;
            }
        }

        public bool IsLeftShiftPressed => _inputSource.IsLeftShiftPressed || _joystickInputSource.IsLeftShiftPressed;

        public bool IsLeftShiftUp => _inputSource.IsLeftShiftUp || _joystickInputSource.IsLeftShiftUp;

        public bool CtrlPressed => _inputSource.CtrlPressed || _joystickInputSource.CtrlPressed;

        public bool CtrlUp => _inputSource.CtrlUp || _joystickInputSource.CtrlUp;

        public bool JumpPressed => _inputSource.JumpPressed || _joystickInputSource.JumpPressed;

        public bool JumpUp => _inputSource.JumpUp || _joystickInputSource.JumpUp;

        public bool JerkPressed => _inputSource.JerkPressed || _joystickInputSource.JerkPressed;

        public bool PickupPressed => _inputSource.PickupPressed || _joystickInputSource.PickupPressed;
    }
}