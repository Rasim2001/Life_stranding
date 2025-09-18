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

        public override void InstallBindings() =>
            BindArrowWorkshopPool();

        private void BindArrowWorkshopPool()
        {
            Container
                .BindInterfacesAndSelfTo<PoolObjects<PickupView>>()
                .AsSingle()
                .WithArguments(PickupView, PickupContainer);
        }
    }
}