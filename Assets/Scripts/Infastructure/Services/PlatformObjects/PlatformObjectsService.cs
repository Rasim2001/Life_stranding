using System.Collections.Generic;
using System.Linq;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;

namespace Infastructure.Services.PlatformObjects
{
    public class PlatformObjectsService : IPlatformObjectsService
    {
        public List<PickupObjectBase> PickupObjects { get; set; } = new List<PickupObjectBase>();

        public bool HasAny<T>() where T : PickupObjectBase =>
            PickupObjects.Any(x => x is T);

        public T Get<T>() where T : PickupObjectBase =>
            PickupObjects.FirstOrDefault(x => x is T) as T;

        public bool IsEmpty() => PickupObjects.Count == 0;
    }
}