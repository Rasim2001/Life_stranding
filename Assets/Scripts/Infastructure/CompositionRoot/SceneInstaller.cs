using Cameras.SpiderCameras;
using Infastructure.Common;
using Infastructure.Common.Pickup;
using Infastructure.Common.StableWorlUpManagement;
using Infastructure.Common.Trajectory;
using Infastructure.Factories;
using Infastructure.Factories.GameFactories;
using Infastructure.PlatformRegistry;
using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.CutScene;
using Infastructure.Services.Defeat;
using Infastructure.Services.Explosion;
using Infastructure.Services.GeneratorLaunchTracker;
using Infastructure.Services.GravityGun;
using Infastructure.Services.Hint;
using Infastructure.Services.Magnet;
using Infastructure.Services.PauseWindow;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.QTE;
using Infastructure.Services.Registries.FlowerRegistry;
using Infastructure.Services.Registries.SpiderRegistry;
using Infastructure.Services.SlowTime;
using Infastructure.Services.Tasks;
using Infastructure.Services.Timer;
using Infastructure.Services.VolumeManagement;
using Infastructure.Services.Window;
using Infastructure.Services.XRay;
using Infastructure.States;
using UI.MVVM.View.Root;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace Infastructure.CompositionRoot
{
    public class SceneInstaller : MonoInstaller
    {
        [SerializeField] private Volume _volume;

        public override void InstallBindings()
        {
            BindUI();

            BindWorld();
        }

        private void BindWorld()
        {
            BindVolume();

            BindVolumeService();

            BindBuildLevelState();

            BindGameFactory();

            BindInputService();

            BindStableWorldUp();

            BindCheckPointInstaller();

            BindExplosionService();

            BindPickupDisplayer();

            BindMagnetService();

            BindPlatformObjectsService();

            BindXRayService();

            BindPlatformRegistryService();

            BindAbilityService();

            BindSpiderRegistryService();

            BindFlowerRegistryService();

            BindTimerService();

            BindDefeatWindowService();

            BindGeneratorLaunchTrackerService();

            BindHitService();

            BindLastChanceQTEService();

            BindSlowTimeService();

            BindCutSceneService();

            BindTrajectoryEndPointDisplayer();

            BindGravityGunDisplayer();

            BindDiFactory();

            BindSpiderCamera();
        }

        private void BindUI()
        {
            BindUIRoot();

            BindUIGameplayRootViewModel();

            BindUIFactory();

            BindWindowService();

            BindTaskPopupCheckerService();

            BindPauseWindowService();

            BindCameraProviderService();
        }

        private void BindSpiderCamera()
        {
            Container
                .Bind<ISpiderCamera>()
                .To<SpiderCamera>()
                .FromComponentInNewPrefabResource(AssetsPath.SpiderCameraPath)
                .AsSingle();
        }

        private void BindSpiderRegistryService() =>
            Container.BindInterfacesAndSelfTo<SpiderRegistryService>().AsSingle();

        private void BindFlowerRegistryService() =>
            Container.BindInterfacesAndSelfTo<FlowerRegistryService>().AsSingle();

        private void BindSlowTimeService()
        {
            Container
                .Bind<ISlowTimeRunner>()
                .To<SlowTimeRunner>()
                .FromComponentInNewPrefabResource(AssetsPath.SlowTimeRunnerPath)
                .AsSingle();
        }

        private void BindGravityGunDisplayer()
        {
            Container
                .Bind<IGravityGunDisplayer>()
                .To<GravityGunDisplayer>()
                .FromComponentInNewPrefabResource(AssetsPath.GravityGunDisplayerPath)
                .AsSingle();
        }

        private void BindDiFactory() =>
            Container.BindInterfacesAndSelfTo<DiFactory>().AsSingle();


        private void BindVolumeService() =>
            Container.BindInterfacesAndSelfTo<VolumeService>().AsSingle();

        private void BindVolume() =>
            Container.Bind<Volume>().FromInstance(_volume).AsSingle();

        private void BindLastChanceQTEService() =>
            Container.BindInterfacesAndSelfTo<LastChanceQTEService>().AsSingle();

        private void BindCameraProviderService() =>
            Container.BindInterfacesAndSelfTo<CameraProviderService>().AsSingle();

        private void BindHitService() =>
            Container.BindInterfacesAndSelfTo<HintReceiverService>().AsSingle();

        private void BindCutSceneService() =>
            Container.BindInterfacesAndSelfTo<CutSceneService>().AsSingle();


        private void BindGeneratorLaunchTrackerService() =>
            Container.BindInterfacesAndSelfTo<GeneratorLaunchTrackerService>().AsSingle();

        private void BindDefeatWindowService() =>
            Container.BindInterfacesAndSelfTo<DefeatWindowService>().AsSingle();

        private void BindTimerService() =>
            Container.BindInterfacesAndSelfTo<TimerService>().AsSingle();

        private void BindPauseWindowService() =>
            Container.BindInterfacesAndSelfTo<PauseWindowService>().AsSingle();

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


        private void BindCheckPointInstaller() =>
            Container.BindInterfacesAndSelfTo<BiospherePointService>().AsSingle();

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

        private void BindTrajectoryEndPointDisplayer()
        {
            Container
                .Bind<ITrajectoryEndPointDisplayer>()
                .To<TrajectoryEndPointDisplayer>()
                .FromComponentInNewPrefabResource(AssetsPath.TrajectoryEndPointDisplayerPath)
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


        private void BindUIGameplayRootViewModel() =>
            Container.BindInterfacesAndSelfTo<UIGameplayRootViewModel>().AsSingle();

        private void BindWindowService() =>
            Container.BindInterfacesAndSelfTo<WindowService>().AsSingle();

        private void BindTaskPopupCheckerService() =>
            Container.BindInterfacesAndSelfTo<TasksService>().AsSingle();
    }
}