using UnityEngine;

namespace Infastructure.Services.PlayerInput
{
    public interface IInputSource
    {
        void Enable();
        void Disable();

        Vector3 InputVector { get; }
        bool LeftMousePressed { get; }
        bool LeftMouseUp { get; }
        bool RightMousePressed { get; }
        bool RightMouseUp { get; }
        bool CenterMousePressed { get; }
        bool CenterMouseUp { get; }
        float ScrollWheelAxis { get; }
        float MouseXAxis { get; }
        float MouseYAxis { get; }
        bool IsLeftShiftPressed { get; }
        bool IsLeftShiftUp { get; }
        bool CtrlPressed { get; }
        bool CtrlUp { get; }
        bool JumpPressed { get; }
        bool JerkPressed { get; }
        bool PickupPressed { get; }
        bool JumpUp { get; }
        bool TabPressed { get; }
    }
}