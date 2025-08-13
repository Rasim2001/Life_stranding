using UnityEngine;

namespace Infastructure.Services.Input
{
    public class InputService : IInputService
    {
        private const string Horizontal = "Horizontal";
        private const string Vertical = "Vertical";

        public Vector3 InputVector =>
            new Vector3(UnityEngine.Input.GetAxis(Horizontal), 0, UnityEngine.Input.GetAxis(Vertical));

        public bool LeftMousePressed => UnityEngine.Input.GetMouseButtonDown(0);
        public bool LeftMouseUp => UnityEngine.Input.GetMouseButtonUp(0);

        public bool RightMousePressed => UnityEngine.Input.GetMouseButtonDown(1);
        public bool RightMouseUp => UnityEngine.Input.GetMouseButtonUp(1);
        public bool CenterMousePressed => UnityEngine.Input.GetMouseButtonDown(2);
        public bool CenterMouseUp => UnityEngine.Input.GetMouseButtonUp(2);

        public float ScrollWheelAxis => UnityEngine.Input.GetAxis("Mouse ScrollWheel");
        public float MouseXAxis => UnityEngine.Input.GetAxis("Mouse X");
        public float MouseYAxis => UnityEngine.Input.GetAxis("Mouse Y");

        public bool IsLeftShiftPressed => UnityEngine.Input.GetKeyDown(KeyCode.LeftShift);
        public bool IsLeftShiftUp => UnityEngine.Input.GetKeyUp(KeyCode.LeftShift);

        public bool CtrlPressed => UnityEngine.Input.GetKeyDown(KeyCode.LeftControl);
        public bool CtrlUp => UnityEngine.Input.GetKeyUp(KeyCode.LeftControl);

        public bool JumpPressed => UnityEngine.Input.GetKeyDown(KeyCode.Space);
        public bool JerkPressed => UnityEngine.Input.GetKeyDown(KeyCode.LeftAlt);
        public bool PickupPressed => UnityEngine.Input.GetKeyDown(KeyCode.E);
    }
}