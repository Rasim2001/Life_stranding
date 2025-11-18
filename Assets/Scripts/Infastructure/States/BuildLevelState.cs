using System;
using Infastructure.Common;
using Infastructure.Factories.GameFactories;
using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerProgressService;
using Infastructure.Services.Restart;
using Infastructure.Services.Timer;
using Infastructure.Services.Window;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController;
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
        private readonly ICutSceneService _cutSceneService;
        private readonly ITimerService _timerService;
        private readonly IWindowService _windowService;
        private ICameraProviderService _cameraProviderService;

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
            ICutSceneService cutSceneService,
            ITimerService timerService,
            ICameraProviderService cameraProviderService
        )
        {
            _cameraProviderService = cameraProviderService;
            _windowService = windowService;
            _restartService = restartService;
            _abilityService = abilityService;
            _cutSceneService = cutSceneService;
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
            _cameraProviderService.SetCamera(Camera.main);

            InitUI();
            InitAll();
        }

        private void InitUI()
        {
            _uiFactory.CreateGamplayRoot();

            if (_restartService.IsRestarting)
                RestartInitialize();
            else
                _windowService.OpenStartSplashScreen();
        }

        private void InitAll()
        {
            _inputService.Initialize();
            _cutSceneService.Clear();
            _timerService.StartTimer();

            InitGameWorld();
        }


        public void Dispose()
        {
        }


        private void RestartInitialize()
        {
            foreach (ProductType productType in _restartService.ExploredProducts)
                _abilityService.PickUpAbility(productType);
        }


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

        private void InitCameraSystem(Spider spiderTransform) =>
            _gameFactory.CreateCameraSystem(spiderTransform);

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