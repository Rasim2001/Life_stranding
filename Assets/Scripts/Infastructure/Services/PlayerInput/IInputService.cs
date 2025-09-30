using UnityEngine;

namespace Infastructure.Services.PlayerInput
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
        bool CtrlPressed { get; }
        bool CtrlUp { get; }
        bool JumpUp { get; }
        bool TabPressed { get; }
        void SetInputSource(IInputSource inputSource);
        bool IsType<T>() where T : IInputSource;
        T GetInputSource<T>();
    }
}