using System;
using Infastructure.Common;
using Infastructure.Factories.GameFactories;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerProgressService;
using Infastructure.Services.Window;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform;
using SpiderController;
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
        private IWindowService _windowService;

        public BuildLevelState(
            IGameFactory gameFactory,
            IStaticDataService staticData,
            IGameUIFactory uiFactory,
            IPersistentProgressService progressService,
            ISceneLoader sceneLoader,
            IBiospherePointService biospherePointService,
            IInputService inputService,
            IWindowService windowService
        )
        {
            _windowService = windowService;
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
            InitUI();
            InitAll();
        }

        private void InitUI()
        {
            _uiFactory.CreateGamplayRoot();

            _windowService.OpenStartSplashScreen();
        }

        private void InitAll()
        {
            _inputService.Initialize();

            InitGameWorld();
        }


        public void Dispose()
        {
        }


        private void InitGameWorld()
        {
            InitCheckPoints();

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