using System;
using Infastructure.Services.PlayerInput.InputSourceRealization;
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
        bool PauseButtonPressed { get; }
        bool AnyActionPressed { get; }
        bool GravityGunPressed { get; }
        bool GravityGunUp { get; }
        bool TeleportPressed { get; }
        T GetInputSource<T>();
        void Initialize();
        event Action<IInputSource> OnJoystickEnableHappend;
        bool IsActiveSource<T>();
        event Action OnJoystickDisableHappend;
        void LockInput();
        void UnlockInput();
    }
}