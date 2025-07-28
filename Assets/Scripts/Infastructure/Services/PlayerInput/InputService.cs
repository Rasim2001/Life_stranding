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
        
        public bool IsLeftShiftPressed => UnityEngine.Input.GetKeyDown(KeyCode.LeftShift);
        public bool IsLeftShiftUp => UnityEngine.Input.GetKeyUp(KeyCode.LeftShift);
    }
}