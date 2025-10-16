using System;
using Infastructure.Common;
using Infastructure.Factories.GameFactories;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerProgressService;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
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
        private readonly ICheckPointService _checkPointService;
        private readonly IInputService _inputService;

        public BuildLevelState(
            IGameFactory gameFactory,
            IStaticDataService staticData,
            IGameUIFactory uiFactory,
            IPersistentProgressService progressService,
            ISceneLoader sceneLoader,
            ICheckPointService checkPointService,
            IInputService inputService
        )
        {
            _gameFactory = gameFactory;
            _staticData = staticData;
            _uiFactory = uiFactory;
            _progressService = progressService;
            _sceneLoader = sceneLoader;
            _checkPointService = checkPointService;
            _inputService = inputService;
        }

        public void Initialize()
        {
            _inputService.Initialize();

            InitGameWorld();
            GoToCheckPoints();
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
        }

        private void InitTerrainScan(Spider spider) =>
            _gameFactory.CreateTerrainScan(spider);

        private void InitBatteryProducts(Spider spider) =>
            _gameFactory.CreateAllBatteryProducts(spider);

        private void InitCheckPoints() =>
            _gameFactory.CreateCheckPointIndicator();


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

        private void GoToCheckPoints() =>
            _checkPointService.GoToNextPoint();
    }
}