using System.Collections.Generic;
using System.Linq;
using PickupObjects;

namespace Infastructure.Services.PlatformObjects
{
    public class PlatformObjectsService : IPlatformObjectsService
    {
        public List<PickupObjectBase> PickupObjects { get; set; } = new List<PickupObjectBase>();

        public bool HasAny<T>() where T : PickupObjectBase =>
            PickupObjects.Any(x => x is T);
    }
}