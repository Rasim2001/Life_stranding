using System.Collections.Generic;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;

namespace Infastructure.Services.PlatformObjects
{
    public interface IPlatformObjectsService
    {
        List<PickupObjectBase> PickupObjects { get; set; }
        bool HasAny<T>() where T : PickupObjectBase;
        bool IsEmpty();
        T Get<T>() where T : PickupObjectBase;
    }
}