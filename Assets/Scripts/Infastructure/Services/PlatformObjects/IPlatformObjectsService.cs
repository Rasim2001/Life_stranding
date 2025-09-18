using System.Collections.Generic;
using PickupObjects;

namespace Infastructure.Services.PlatformObjects
{
    public interface IPlatformObjectsService
    {
        List<PickupObjectBase> PickupObjects { get; set; }
        bool HasAny<T>() where T : PickupObjectBase;
    }
}