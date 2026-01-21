using System;
using Infastructure.Common;
using Infastructure.Factories.GameFactories;
using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerProgressService;
using Infastructure.Services.ProgressWatchers;
using Infastructure.Services.Restart;
using Infastructure.Services.SaveLoadService;
using Infastructure.Services.StartGame;
using Infastructure.Services.Tasks;
using Infastructure.Services.Timer;
using Infastructure.Services.Window;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController;
using SpiderController.UI.Health;
using UnityEngine;
using Zenject;

namespace Infastructure.States
{
    public class BuildLevelState : IInitializable, IDisposable
    {
        private readonly IGameFactory _gameFactory;
        private readonly IStaticDataService _staticData;
        private readonly IGameUIFactory _uiFactory;
        private readonly IPersistentProgressService _progressService;
        private readonly ISceneLoader _sceneLoader;
        private readonly IBiospherePointService _biospherePointService;
        private readonly IInputService _inputService;
        private readonly IRestartService _restartService;
        private readonly IAbilityService _abilityService;
        private readonly ITimerService _timerService;
        private readonly IWindowService _windowService;
        private ICameraProviderService _cameraProviderService;
        private readonly IProgressWatchersService _progressWatchersService;
        private readonly ITasksService _tasksService;
        private readonly IStartGameReceiver _startGameReceiver;
        private readonly ISaveLoadService _saveLoadService;

        public BuildLevelState(
            IGameFactory gameFactory,
            IStaticDataService staticData,
            IGameUIFactory uiFactory,
            IPersistentProgressService progressService,
            ISceneLoader sceneLoader,
            IBiospherePointService biospherePointService,
            IInputService inputService,
            IWindowService windowService,
            IRestartService restartService,
            IAbilityService abilityService,
            ITimerService timerService,
            ICameraProviderService cameraProviderService,
            IProgressWatchersService progressWatchersService,
            ITasksService tasksService,
            IStartGameReceiver startGameReceiver,
            ISaveLoadService saveLoadService
        )
        {
            _cameraProviderService = cameraProviderService;
            _progressWatchersService = progressWatchersService;
            _tasksService = tasksService;
            _startGameReceiver = startGameReceiver;
            _saveLoadService = saveLoadService;
            _windowService = windowService;
            _restartService = restartService;
            _abilityService = abilityService;
            _timerService = timerService;
            _gameFactory = gameFactory;
            _staticData = staticData;
            _uiFactory = uiFactory;
            _progressService = progressService;
            _sceneLoader = sceneLoader;
            _biospherePointService = biospherePointService;
            _inputService = inputService;
        }

        public void Initialize()
        {
            InitServices();
            InitGameplayRootUI();
        }

        private void InitGameplayRootUI()
        {
            _uiFactory.CreateGamplayRoot();

            if (_restartService.IsRestarting)
                Restart();
            else
                _windowService.OpenStartSplashScreen();
        }

        private void InitServices()
        {
            _startGameReceiver.OnStartGameHappened += InitGameWorld;

            _cameraProviderService.SetCamera(Camera.main);
            _tasksService.Initialize();
            _abilityService.Initialize();
            _inputService.Initialize();
            _timerService.Initialize();

            _progressWatchersService.Clear();
        }


        public void Dispose()
        {
            _startGameReceiver.OnStartGameHappened -= InitGameWorld;

            _abilityService.Dispose();
            _tasksService.Dispose();
        }


        private void Restart() =>
            _startGameReceiver.StartGame();


        private void InitGameWorld()
        {
            InitCheckPoints();
            InitGenerators();

            Flower flower = InitFlower();
            Spider spider = InitSpider(flower);

            InitHUD(flower, spider);
            InitTerrainScan(spider);
            InitCameraSystem(spider);
            InitStartGameScene(spider);

            InitBatteryProducts(spider);
            InitElephantProducts(spider);
            InitEnergyProducts();
            InitSkillProducts();

            InitLastChanceRoot(flower, spider);

            _saveLoadService.InitLoadingProgress();
        }

        private void InitLastChanceRoot(Flower flower, Spider spider)
        {
            SpiderUI spiderUI = spider.GetComponent<SpiderUI>();

            _uiFactory.CreateLastChanceRoot(flower, spiderUI.LastChanceBarUI);
        }

        private void InitGenerators() =>
            _gameFactory.CreateAllGenerators();

        private void InitTerrainScan(Spider spider) =>
            _gameFactory.CreateTerrainScan(spider);

        private void InitBatteryProducts(Spider spider) =>
            _gameFactory.CreateAllBatteryProducts(spider);

        private void InitCheckPoints() =>
            _gameFactory.CreateCheckPoints();

        private Spider InitSpider(Flower flower) =>
            _gameFactory.CreateSpider(flower);

        private void InitCameraSystem(Spider spider) =>
            _gameFactory.CreateCameraSystem(spider);

        private Flower InitFlower() =>
            _gameFactory.CreateFlower();

        private void InitHUD(Flower flower, Spider spider) =>
            _gameFactory.CreateHUD(flower, spider);

        private void InitStartGameScene(Spider spider) =>
            _gameFactory.CreateStartGameCutSceneTimeline(spider);

        private void InitEnergyProducts() =>
            _gameFactory.CreateEnergyProducts();

        private void InitElephantProducts(Spider spider) =>
            _gameFactory.CreateElephantProduct(spider);

        private void InitSkillProducts() =>
            _gameFactory.CreateSkillProducts();
    }
}