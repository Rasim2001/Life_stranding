using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Infastructure.Services.PlayerInput.InputSourceRealization
{
    public class PlayerInputSource : IInputSource
    {
        private const string Horizontal = "Horizontal";
        private const string Vertical = "Vertical";
        private const string MouseScrollWheel = "Mouse ScrollWheel";
        private const string MouseX = "Mouse X";
        private const string MouseY = "Mouse Y";

        public void Enable()
        {
        }

        public void Disable() =>
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());

        public bool PauseButtonPressed => Input.GetKeyDown(KeyCode.Escape);

        public Vector3 InputVector =>
            new Vector3(Input.GetAxis(Horizontal), 0, Input.GetAxis(Vertical));

        public bool LeftMousePressed => Input.GetMouseButtonDown(0);
        public bool LeftMouseUp => Input.GetMouseButtonUp(0);

        public bool RightMousePressed => Input.GetMouseButtonDown(1);
        public bool RightMouseUp => Input.GetMouseButtonUp(1);
        public bool CenterMousePressed => Input.GetMouseButtonDown(2);
        public bool CenterMouseUp => Input.GetMouseButtonUp(2);

        public float ScrollWheelAxis => Input.GetAxis(MouseScrollWheel);
        public float MouseXAxis => Input.GetAxis(MouseX);
        public float MouseYAxis => Input.GetAxis(MouseY);

        public bool IsLeftShiftPressed => Input.GetKeyDown(KeyCode.LeftShift);
        public bool IsLeftShiftUp => Input.GetKeyUp(KeyCode.LeftShift);

        public bool CtrlPressed => Input.GetKeyDown(KeyCode.LeftControl);
        public bool CtrlUp => Input.GetKeyUp(KeyCode.LeftControl);

        public bool JumpPressed => Input.GetKeyDown(KeyCode.Space);
        public bool JumpUp => Input.GetKeyUp(KeyCode.Space);
        public bool TabPressed => Input.GetKeyUp(KeyCode.Tab);
        public bool JerkPressed => Input.GetKeyDown(KeyCode.LeftAlt);
        public bool PickupPressed => Input.GetKeyDown(KeyCode.E);
    }
}