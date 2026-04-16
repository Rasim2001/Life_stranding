using System;
using Cameras.SpiderCameras;
using Infastructure.Common.StableWorlUpManagement;
using Infastructure.Factories.GameFactories;
using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.ProgressWatchers;
using Infastructure.Services.Restart;
using Infastructure.Services.SaveLoadService;
using Infastructure.Services.StartGame;
using Infastructure.Services.Tasks;
using Infastructure.Services.Timer;
using Infastructure.Services.Window;
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
        private readonly IGameUIFactory _uiFactory;
        private readonly IInputService _inputService;
        private readonly IRestartService _restartService;
        private readonly IAbilityService _abilityService;
        private readonly ITimerService _timerService;
        private readonly IWindowService _windowService;
        private readonly ICameraProviderService _cameraProviderService;
        private readonly IProgressWatchersService _progressWatchersService;
        private readonly ITasksService _tasksService;
        private readonly IStartGameReceiver _startGameReceiver;
        private readonly ISaveLoadService _saveLoadService;
        private readonly ISpiderCamera _spiderCamera;
        private readonly IStableWorldUp _stableWorldUp;

        public BuildLevelState(
            IGameFactory gameFactory,
            IGameUIFactory uiFactory,
            IInputService inputService,
            IWindowService windowService,
            IRestartService restartService,
            IAbilityService abilityService,
            ITimerService timerService,
            ICameraProviderService cameraProviderService,
            IProgressWatchersService progressWatchersService,
            ITasksService tasksService,
            IStartGameReceiver startGameReceiver,
            ISaveLoadService saveLoadService,
            ISpiderCamera spiderCamera,
            IStableWorldUp stableWorldUp
        )
        {
            _cameraProviderService = cameraProviderService;
            _progressWatchersService = progressWatchersService;
            _tasksService = tasksService;
            _startGameReceiver = startGameReceiver;
            _saveLoadService = saveLoadService;
            _spiderCamera = spiderCamera;
            _stableWorldUp = stableWorldUp;
            _windowService = windowService;
            _restartService = restartService;
            _abilityService = abilityService;
            _timerService = timerService;
            _gameFactory = gameFactory;
            _uiFactory = uiFactory;
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
            _stableWorldUp.Initialize();

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

            _spiderCamera.Initialize();
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