using System;
using SpiderController.StateMachine;

namespace Infastructure.Services.Magnet
{
    public interface IMagnetFreezingService
    {
        void Freeze();
        void Unfreeze();
        void Initialize(StateMachineData stateMachineData);
        bool IsActive { get; }
        event Action<bool> OnFreezActiveChanged;
        void FreezeForAiming();
        void UnfreezeForAiming();
    }
}