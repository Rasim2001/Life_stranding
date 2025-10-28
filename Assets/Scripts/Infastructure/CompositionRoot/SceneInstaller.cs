using Infastructure.Common;
using Infastructure.Common.Pickup;
using Infastructure.Common.StableWorlUpManagement;
using Infastructure.Factories.GameFactories;
using Infastructure.PlatformRegistry;
using Infastructure.Services.Ability;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.CutScene;
using Infastructure.Services.Explosion;
using Infastructure.Services.Magnet;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Infastructure.Services.XRay;
using Infastructure.States;
using UI.MVVM.View.Root;
using Zenject;

namespace Infastructure.CompositionRoot
{
    public class SceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindUI();

            BindWorld();
        }

        private void BindWorld()
        {
            BindBuildLevelState();

            BindGameFactory();

            BindInputService();

            BindStableWorldUp();

            BindCheckPointInstaller();

            BindCutSceneService();

            BindExplosionService();

            BindPickupDisplayer();

            BindMagnetService();

            BindPlatformObjectsService();

            BindXRayService();

            BindPlatformRegistryService();

            BindAbilityService();
        }

        private void BindUI()
        {
            BindUIRoot();

            BindUIGameplayRootViewModel();

            BindUIFactory();

            BindWindowService();

            BindEventSystemSelector();
        }

        private void BindAbilityService() =>
            Container.BindInterfacesAndSelfTo<AbilityService>().AsSingle();

        private void BindPlatformRegistryService() =>
            Container.BindInterfacesAndSelfTo<PlatformRegistryService>().AsSingle();

        private void BindXRayService() =>
            Container.BindInterfacesAndSelfTo<XRayService>().AsSingle();

        private void BindPlatformObjectsService() =>
            Container.BindInterfacesAndSelfTo<PlatformObjectsService>().AsSingle();

        private void BindMagnetService() =>
            Container.BindInterfacesAndSelfTo<MagnetFreezingService>().AsSingle();

        private void BindExplosionService() =>
            Container.BindInterfacesAndSelfTo<ExplosionService>().AsSingle();

        private void BindCutSceneService() =>
            Container.BindInterfacesAndSelfTo<CutSceneService>().AsSingle();

        private void BindCheckPointInstaller() =>
            Container.BindInterfacesAndSelfTo<CheckPointService>().AsSingle();

        private void BindStableWorldUp()
        {
            Container
                .Bind<IStableWorldUp>()
                .To<StableWorldUp>()
                .FromComponentInNewPrefabResource(AssetsPath.StableWorldUpPath)
                .AsSingle();
        }

        private void BindInputService() =>
            Container.BindInterfacesAndSelfTo<InputService>().AsSingle();


        private void BindBuildLevelState() =>
            Container.BindInterfacesAndSelfTo<BuildLevelState>().AsSingle().NonLazy();

        private void BindUIFactory() =>
            Container.BindInterfacesAndSelfTo<GameUIFactory>().AsSingle();

        private void BindPickupDisplayer()
        {
            Container
                .Bind<IPickupDisplayer>()
                .To<PickupDisplayer>()
                .FromComponentInNewPrefabResource(AssetsPath.PickupDisplayerPath)
                .AsSingle();
        }

        private void BindGameFactory() =>
            Container.BindInterfacesAndSelfTo<GameFactory>().AsSingle();


        private void BindUIRoot()
        {
            Container
                .Bind<IUIRoot>()
                .To<UIRoot>()
                .FromComponentInNewPrefabResource(AssetsPath.UIRootPath)
                .AsSingle();
        }

        private void BindEventSystemSelector()
        {
            Container
                .Bind<IEventSystemSelector>()
                .To<EventSystemSelector>()
                .FromComponentInNewPrefabResource(AssetsPath.EventSystemPath)
                .AsSingle();
        }

        private void BindUIGameplayRootViewModel() =>
            Container.BindInterfacesAndSelfTo<UIGameplayRootViewModel>().AsSingle();

        private void BindWindowService() =>
            Container.BindInterfacesAndSelfTo<WindowService>().AsSingle();
    }
}