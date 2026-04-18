using System;
using PickupObjects.PickUpOnPlatform.FlowerManagement;

namespace Infastructure.Services.Registries.FlowerRegistry
{
    public interface IFlowerRegistryService
    {
        event Action OnFlowerInitialized;
        Flower Flower { get; }
        void RegisterFlower(Flower flower);
    }
}