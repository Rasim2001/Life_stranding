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
        public bool IsLeftShiftPressed { get; }
        public bool IsLeftShiftUp { get; }
        public bool CtrlPressed { get; }
        public bool CtrlUp { get; }
        public bool JumpPressed { get; }
        public bool JerkPressed { get; }
        public bool PickupPressed { get; }
    }
}