using SpiderController.StateMachine;

namespace Infastructure.Services.Magnet
{
    public interface IMagnetFreezingService
    {
        void Freeze();
        void Unfreeze();
        void Initialize(StateMachineData stateMachineData);
    }
}