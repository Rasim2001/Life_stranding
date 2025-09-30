using System;
using UnityEngine;
using Zenject;

namespace Infastructure.Services.PlayerInput
{
    public class InputService : IInputService, IInitializable, IDisposable
    {
        private IInputSource _inputSource;

        public void Initialize() =>
            SetInputSource(new PlayerInputSource());

        public void Dispose() =>
            _inputSource.Disable();

        public void SetInputSource(IInputSource inputSource)
        {
            _inputSource = inputSource;
            _inputSource.Enable();
        }

        public T GetInputSource<T>() =>
            (T)_inputSource;

        public bool IsType<T>() where T : IInputSource =>
            _inputSource is T;

        public Vector3 InputVector => _inputSource.InputVector;

        public bool TabPressed => _inputSource.TabPressed;

        public bool LeftMousePressed => _inputSource.LeftMousePressed;

        public bool LeftMouseUp => _inputSource.LeftMouseUp;

        public bool RightMousePressed => _inputSource.RightMousePressed;

        public bool RightMouseUp => _inputSource.RightMouseUp;

        public bool CenterMousePressed => _inputSource.CenterMousePressed;

        public bool CenterMouseUp => _inputSource.CenterMouseUp;

        public float ScrollWheelAxis => _inputSource.ScrollWheelAxis;

        public float MouseXAxis => _inputSource.MouseXAxis;

        public float MouseYAxis => _inputSource.MouseYAxis;

        public bool IsLeftShiftPressed => _inputSource.IsLeftShiftPressed;

        public bool IsLeftShiftUp => _inputSource.IsLeftShiftUp;

        public bool CtrlPressed => _inputSource.CtrlPressed;

        public bool CtrlUp => _inputSource.CtrlUp;

        public bool JumpPressed => _inputSource.JumpPressed;

        public bool JumpUp => _inputSource.JumpUp;

        public bool JerkPressed => _inputSource.JerkPressed;

        public bool PickupPressed => _inputSource.PickupPressed;
    }
}