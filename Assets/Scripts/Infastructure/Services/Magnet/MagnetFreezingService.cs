using Infastructure.Services.Ability;
using Infastructure.Services.PlatformObjects;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;

namespace Infastructure.Services.Magnet
{
    public class MagnetFreezingService : IMagnetFreezingService
    {
        private readonly IPlatformObjectsService _platformObjectsService;
        private readonly IAbilityService _abilityService;

        public MagnetFreezingService(IPlatformObjectsService platformObjectsService, IAbilityService abilityService)
        {
            _abilityService = abilityService;
            _platformObjectsService = platformObjectsService;
        }


        public void Freeze()
        {
            if (!_abilityService.IsExploredAbility(ProductType.MagnetSkillProduct))
                return;

            foreach (PickupObjectBase pickupObject in _platformObjectsService.PickupObjects)
                pickupObject.IsFreezingOnPlatform = true;
        }

        public void Unfreeze()
        {
            if (!_abilityService.IsExploredAbility(ProductType.MagnetSkillProduct))
                return;

            foreach (PickupObjectBase pickupObject in _platformObjectsService.PickupObjects)
                pickupObject.IsFreezingOnPlatform = false;
        }
    }
}