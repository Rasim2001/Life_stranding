using UnityEngine;

namespace Infastructure.Services.Input
{
    public interface IInputService
    {
        Vector3 InputVector { get; }
        bool IsLeftShiftPressed { get; }
        bool IsLeftShiftUp { get; }
        bool LeftMousePressed { get; }
        bool LeftMouseUp { get; }
        bool RightMousePressed { get; }
        bool RightMouseUp { get; }
        float ScrollWheelAxis { get; }
        float MouseXAxis { get; }
        bool JumpPressed { get; }
        float MouseYAxis { get; }
        bool JerkPressed { get; }
        bool PickupPressed { get; }
        bool CenterMousePressed { get; }
        bool CenterMouseUp { get; }
    }
}