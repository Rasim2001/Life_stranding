using System;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController;

namespace Infastructure.Services.SpiderTrack
{
    public interface ISpiderTrackService
    {
        Spider Spider { get; set; }
        Flower Flower { get; set; }
        string GetDistanceToGoal();
        event Action OnSpiderInitialized;
        event Action OnFlowerInitialized;
    }
}