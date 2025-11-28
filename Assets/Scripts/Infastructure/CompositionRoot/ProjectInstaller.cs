using Infastructure.Common;
using Infastructure.Common.Pickup;
using Infastructure.Factories.ProjectFactories;
using Infastructure.Localization;
using Infastructure.Services.CursorVisible;
using Infastructure.Services.CutScene;
using Infastructure.Services.Pause;
using Infastructure.Services.PlayerProgressService;
using Infastructure.Services.QuitApplication;
using Infastructure.Services.Restart;
using Infastructure.Services.SaveLoadService;
using Infastructure.States;
using Infastructure.StaticData.StaticDataService;
using UI;
using UI.Curtain;
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

            BindPersistentProgressService();

            BindSaveLoadService();

            BindCurtainRoot();

            BindPauseService();

            BindRestartService();

            BindCutSceneService();

            BindCursorVisibleService();

            BindLocalizationService();

            BindQuitGameService();
        }

        private void BindQuitGameService() =>
            Container.BindInterfacesAndSelfTo<QuitGameService>().AsSingle();

        private void BindLocalizationService() =>
            Container.BindInterfacesAndSelfTo<LocalizationService>().AsSingle();

        private void BindCursorVisibleService() =>
            Container.BindInterfacesAndSelfTo<CursorVisibleService>().AsSingle();

        private void BindCutSceneService() =>
            Container.BindInterfacesAndSelfTo<CutSceneService>().AsSingle();

        private void BindRestartService() =>
            Container.BindInterfacesAndSelfTo<RestartService>().AsSingle();

        private void BindPauseService() =>
            Container.BindInterfacesAndSelfTo<PauseService>().AsSingle();


        private void BindPersistentProgressService() =>
            Container.BindInterfacesAndSelfTo<PersistentProgressService>().AsSingle();

        private void BindSaveLoadService() =>
            Container.BindInterfacesAndSelfTo<SaveLoadService>().AsSingle();


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


        private void BindCurtainRoot()
        {
            Container
                .Bind<ICurtainRoot>()
                .To<CurtainRoot>()
                .FromComponentInNewPrefabResource(AssetsPath.CurtainRootPath)
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