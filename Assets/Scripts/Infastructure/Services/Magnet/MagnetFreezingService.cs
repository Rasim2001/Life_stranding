using Infastructure.Services.PlatformObjects;
using PickupObjects;

namespace Infastructure.Services.Magnet
{
    public class MagnetFreezingService : IMagnetFreezingService
    {
        private readonly IPlatformObjectsService _platformObjectsService;

        public MagnetFreezingService(IPlatformObjectsService platformObjectsService) =>
            _platformObjectsService = platformObjectsService;


        public void Freeze()
        {
            foreach (PickupObjectBase pickupObject in _platformObjectsService.PickupObjects)
                pickupObject.IsFreezingOnPlatform = true;
        }

        public void Unfreeze()
        {
            foreach (PickupObjectBase pickupObject in _platformObjectsService.PickupObjects)
                pickupObject.IsFreezingOnPlatform = false;
        }
    }
}