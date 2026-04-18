using System;
using PickupObjects.PickUpOnPlatform.FlowerManagement;

namespace Infastructure.Services.Registries.FlowerRegistry
{
    public class FlowerRegistryService : IFlowerRegistryService
    {
        public event Action OnFlowerInitialized;
        public Flower Flower { get; private set; }

        public void RegisterFlower(Flower flower)
        {
            Flower = flower;
            OnFlowerInitialized?.Invoke();
        }
    }
}