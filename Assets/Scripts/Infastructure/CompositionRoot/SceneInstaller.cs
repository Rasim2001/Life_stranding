using Infastructure.Common;
using Infastructure.Factories.GameFactories;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.CutScene;
using Infastructure.Services.Explosion;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window.GameWindowService;
using Infastructure.States;
using UnityEngine.Rendering;
using Zenject;

namespace Infastructure.CompositionRoot
{
    public class SceneInstaller : MonoInstaller
    {
        public Volume GlobalVolume;

        public override void InstallBindings()
        {
            BindBuildLevelState();

            BindWindowService();

            BindGameFactory();

            BindUIFactory();

            BindInputService();

            BindStableWorldUp();

            BindCheckPointInstaller();

            BindCutSceneService();

            BindGlobalVolume();

            BindExplosionService();
        }

        private void BindExplosionService() =>
            Container.BindInterfacesAndSelfTo<ExplosionService>().AsSingle();

        private void BindGlobalVolume() =>
            Container.Bind<Volume>().FromInstance(GlobalVolume).AsSingle();

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


        private void BindGameFactory() =>
            Container.BindInterfacesAndSelfTo<GameFactory>().AsSingle();

        private void BindWindowService() =>
            Container.BindInterfacesAndSelfTo<GameWindowService>().AsSingle();
    }
}