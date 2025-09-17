using System.Collections.Generic;
using PickupObjects;

namespace Infastructure.Services.Magnet
{
    public class MagnetFreezingService : IMagnetFreezingService
    {
        private readonly List<PickupObjectBase> _pickupObjects = new List<PickupObjectBase>();

        public void Add(PickupObjectBase pickupObject) =>
            _pickupObjects.Add(pickupObject);

        public void Remove(PickupObjectBase pickupObject) =>
            _pickupObjects.Remove(pickupObject);

        public void Freeze()
        {
            foreach (PickupObjectBase pickupObject in _pickupObjects)
                pickupObject.IsFreezingOnPlatform = true;
        }

        public void Unfreeze()
        {
            foreach (PickupObjectBase pickupObject in _pickupObjects)
                pickupObject.IsFreezingOnPlatform = false;
        }
    }
}