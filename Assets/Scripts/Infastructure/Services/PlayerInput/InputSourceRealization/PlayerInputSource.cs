using UnityEngine;

namespace Infastructure.Services.PlayerInput
{
    public class PlayerInputSource : IInputSource
    {
        private const string Horizontal = "Horizontal";
        private const string Vertical = "Vertical";

        public Vector3 InputVector =>
            new Vector3(Input.GetAxis(Horizontal), 0, Input.GetAxis(Vertical));

        public bool LeftMousePressed => Input.GetMouseButtonDown(0);
        public bool LeftMouseUp => Input.GetMouseButtonUp(0);

        public bool RightMousePressed => Input.GetMouseButtonDown(1);
        public bool RightMouseUp => Input.GetMouseButtonUp(1);
        public bool CenterMousePressed => Input.GetMouseButtonDown(2);
        public bool CenterMouseUp => Input.GetMouseButtonUp(2);

        public float ScrollWheelAxis => Input.GetAxis("Mouse ScrollWheel");
        public float MouseXAxis => Input.GetAxis("Mouse X");
        public float MouseYAxis => Input.GetAxis("Mouse Y");

        public bool IsLeftShiftPressed => Input.GetKeyDown(KeyCode.LeftShift);
        public bool IsLeftShiftUp => Input.GetKeyUp(KeyCode.LeftShift);

        public bool CtrlPressed => Input.GetKeyDown(KeyCode.LeftControl);
        public bool CtrlUp => Input.GetKeyUp(KeyCode.LeftControl);

        public bool JumpPressed => Input.GetKeyDown(KeyCode.Space);
        public bool JerkPressed => Input.GetKeyDown(KeyCode.LeftAlt);
        public bool PickupPressed => Input.GetKeyDown(KeyCode.E);
    }
}