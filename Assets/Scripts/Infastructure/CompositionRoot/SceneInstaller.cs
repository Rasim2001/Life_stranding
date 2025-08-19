using Infastructure.Common;
using Infastructure.Factories.GameFactories;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.Input;
using Infastructure.Services.Window.GameWindowService;
using Infastructure.States;
using Zenject;

namespace Infastructure.CompositionRoot
{
    public class SceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindBuildLevelState();

            BindWindowService();

            BindGameFactory();

            BindUIFactory();

            BindInputService();

            BindStableWorldUp();

            BindCheckPointInstaller();
        }

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


        private void BindGameFactory() =>
            Container.BindInterfacesAndSelfTo<GameFactory>().AsSingle();

        private void BindWindowService() =>
            Container.BindInterfacesAndSelfTo<GameWindowService>().AsSingle();
    }
}