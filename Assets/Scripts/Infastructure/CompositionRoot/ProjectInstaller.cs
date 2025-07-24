using Infastructure.Factories.ProjectFactories;
using Infastructure.Services.PlayerProgressService;
using Infastructure.Services.SaveLoadService;
using Infastructure.Services.Window.ProjectWindowService;
using Infastructure.States;
using Infastructure.StaticData.StaticDataService;
using Zenject;

namespace Infastructure.CompositionRoot
{
    public class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindGameBootstraperFactory();

            BindCoroutineRunner();

            BindSceneLoader();

            BindGameStateMachine();

            BindStaticDataService();

            BindProjectUIFactory();

            BindProjectWindowService();

            BindPersistentProgressService();

            BindSaveLoadService();
        }


        private void BindPersistentProgressService() =>
            Container.BindInterfacesAndSelfTo<PersistentProgressService>().AsSingle();

        private void BindSaveLoadService() =>
            Container.BindInterfacesAndSelfTo<SaveLoadService>().AsSingle();

        private void BindProjectWindowService() =>
            Container.BindInterfacesAndSelfTo<ProjectWindowService>().AsSingle();

        private void BindProjectUIFactory() =>
            Container.BindInterfacesAndSelfTo<ProjectUIFactory>().AsSingle();

        private void BindStaticDataService() =>
            Container.BindInterfacesAndSelfTo<StaticDataService>().AsSingle();

        private void BindGameBootstraperFactory()
        {
            Container
                .BindFactory<GameBootstrapper, GameBootstrapper.Factory>()
                .FromComponentInNewPrefabResource(AssetsPath.GameBootstrapperPath);
        }

        private void BindCoroutineRunner()
        {
            Container
                .Bind<ICoroutineRunner>()
                .To<CoroutineRunner>()
                .FromComponentInNewPrefabResource(AssetsPath.CoroutineRunnerPath)
                .AsSingle();
        }

        private void BindSceneLoader() =>
            Container.BindInterfacesAndSelfTo<SceneLoader>().AsSingle();

        private void BindGameStateMachine()
        {
            Container
                .Bind<IStateMachine>()
                .FromSubContainerResolve()
                .ByInstaller<GameStateMachineInstaller>()
                .AsSingle();
        }
    }
}