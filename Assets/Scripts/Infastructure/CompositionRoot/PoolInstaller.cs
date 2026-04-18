using HUD;
using Infastructure.Common.Pickup;
using Infastructure.Services.Pool;
using PickupObjects.Teleports;
using UnityEngine;
using Zenject;

namespace Infastructure.CompositionRoot
{
    public class PoolInstaller : MonoInstaller
    {
        public Teleport Teleport;
        public Transform TeleportContainer;

        public PickupView PickupView;
        public Transform PickupContainer;

        public XRayOccluderUI OccluderUI;
        public Transform OccluderContainer;

        public override void InstallBindings()
        {
            BindArrowWorkshopPool();

            BindXRayOccluder();

            BindTeleportPools();
        }

        private void BindTeleportPools()
        {
            Container
                .BindInterfacesAndSelfTo<PoolObjects<Teleport>>()
                .AsSingle()
                .WithArguments(Teleport, TeleportContainer);
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