using UnityEngine;

namespace Infastructure.Services.PlayerInput
{
    public class CutSceneInputSource : IInputSource
    {
        public Vector3 InputVector { get; set; }
        public bool LeftMousePressed { get; }
        public bool LeftMouseUp { get; }
        public bool RightMousePressed { get; }
        public bool RightMouseUp { get; }
        public bool CenterMousePressed { get; }
        public bool CenterMouseUp { get; }
        public float ScrollWheelAxis { get; }
        public float MouseXAxis { get; }
        public float MouseYAxis { get; }
        public bool IsLeftShiftPressed
        {
            get
            {
                if (_isLeftShiftPressed)
                    IsLeftShiftUp = false;

                return _isLeftShiftPressed;
            }
            set => _isLeftShiftPressed = value;
        }
        public bool IsLeftShiftUp
        {
            get
            {
                if (_isLeftShiftUp)
                {
                    _isLeftShiftUp = false;
                    IsLeftShiftPressed = false;

                    return true;
                }

                return false;
            }
            set => _isLeftShiftUp = value;
        }
        public bool CtrlPressed { get; }
        public bool CtrlUp { get; }
        public bool JumpPressed
        {
            get
            {
                if (_jumpPressed)
                {
                    _jumpPressed = false;

                    return true;
                }

                return false;
            }
            set => _jumpPressed = value;
        }
        public bool JumpUp { get; set; }
        public bool TabPressed { get; }
        public bool JerkPressed { get; }
        public bool PickupPressed { get; }


        private bool _jumpPressed;
        private bool _isLeftShiftPressed;
        private bool _isLeftShiftUp;
    }
}