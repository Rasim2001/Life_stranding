using Cameras.SpiderCameras;
using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CutScene;
using Infastructure.Services.Magnet;
using Infastructure.Services.Pause;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Infastructure.StaticData.StaticDataService;

namespace SpiderController
{
    public class SpiderServiceContext
    {
        public ISpiderCamera SpiderCamera { get; }
        public IInputService InputService { get; }
        public IStaticDataService StaticDataService { get; }
        public ICameraProviderService CameraProviderService { get; }
        public IAbilityService AbilityService { get; }
        public IWindowService WindowService { get; }
        public IEventSystemSelector SystemSelector { get; }
        public IPauseService PauseService { get; }
        public ICutSceneService CutSceneService { get; }
        public IMagnetFreezingService MagnetFreezingService { get; set; }
        public IPlatformObjectsService PlatformObjectsService { get; set; }


        public SpiderServiceContext(
            ISpiderCamera spiderCamera,
            IInputService inputService,
            IStaticDataService staticDataService,
            ICameraProviderService cameraProviderService,
            IAbilityService abilityService,
            IWindowService windowService,
            IEventSystemSelector systemSelector,
            IPauseService pauseService,
            ICutSceneService cutSceneService,
            IMagnetFreezingService magnetFreezingService, IPlatformObjectsService platformObjectsService)
        {
            SpiderCamera = spiderCamera;
            InputService = inputService;
            StaticDataService = staticDataService;
            CameraProviderService = cameraProviderService;
            AbilityService = abilityService;
            WindowService = windowService;
            SystemSelector = systemSelector;
            PauseService = pauseService;
            CutSceneService = cutSceneService;
            MagnetFreezingService = magnetFreezingService;
            PlatformObjectsService = platformObjectsService;
        }
    }
}