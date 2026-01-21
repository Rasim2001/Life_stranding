using UnityEngine;

namespace Infastructure.Services.PlayerInput.InputSourceRealization
{
    public class CutSceneInputSource : IInputSource
    {
        public void Enable()
        {
        }

        public void Disable()
        {
        }

        public bool PauseButtonPressed { get; }

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

        public bool JumpUp { get; set; }
        public bool TabPressed { get; }
        public bool GravityGunPressed { get; }
        public bool GravityGunUp { get; }

        public bool AnyKeyPressed() =>
            false;

        public bool JerkPressed { get; }
        public bool PickupPressed { get; }
    }
}