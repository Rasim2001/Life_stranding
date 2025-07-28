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
    }
}