using System.Collections.Generic;
using PickupObjects.PickUpOnPlatform;
using SpiderController;
using SpiderController.Platform;

namespace Infastructure.Services.PlatformObjects
{
    public interface IPlatformObjectsService
    {
        IReadOnlyList<PickupObjectBase> PickupObjects { get; }
        public void Initialize(SpiderStateContext stateContext, PlatformSelector platformSelector);

        void Add(PickupObjectBase obj);
        void Remove(PickupObjectBase obj);
        void Throw(PickupObjectBase obj);

        bool HasAny<T>() where T : PickupObjectBase;
        bool IsEmpty();
        T Get<T>() where T : PickupObjectBase;

        void Update();
        void FixedUpdate();
    }
}