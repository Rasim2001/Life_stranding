using PickupObjects;

namespace Infastructure.Services.Magnet
{
    public interface IMagnetFreezingService
    {
        void Add(PickupObjectBase pickupObject);
        void Remove(PickupObjectBase pickupObject);
        void Freeze();
        void Unfreeze();
    }
}