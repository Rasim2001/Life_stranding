using HUD;
using Infastructure.Common.Pickup;
using Infastructure.Services.Pool;
using UnityEngine;
using Zenject;

namespace Infastructure.CompositionRoot
{
    public class PoolInstaller : MonoInstaller
    {
        public PickupView PickupView;
        public Transform PickupContainer;

        public XRayOccluderUI OccluderUI;
        public Transform OccluderContainer;

        public override void InstallBindings()
        {
            BindArrowWorkshopPool();

            BindXRayOccluder();
        }

        private void BindArrowWorkshopPool()
        {
            Container
                .BindInterfacesAndSelfTo<PoolObjects<PickupView>>()
                .AsSingle()
                .WithArguments(PickupView, PickupContainer);
        }

        private void BindXRayOccluder()
        {
            Container
                .BindInterfacesAndSelfTo<PoolObjects<XRayOccluderUI>>()
                .AsSingle()
                .WithArguments(OccluderUI, OccluderContainer);
        }
    }
}