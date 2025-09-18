using System;
using Infastructure.Common;
using Infastructure.Factories.GameFactories;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.PlayerProgressService;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
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

        public BuildLevelState(
            IGameFactory gameFactory,
            IStaticDataService staticData,
            IGameUIFactory uiFactory,
            IPersistentProgressService progressService,
            ISceneLoader sceneLoader,
            ICheckPointService checkPointService
        )
        {
            _gameFactory = gameFactory;
            _staticData = staticData;
            _uiFactory = uiFactory;
            _progressService = progressService;
            _sceneLoader = sceneLoader;
            _checkPointService = checkPointService;
        }

        public void Initialize()
        {
            if (!_sceneLoader.IsGameScene())
                return;

            /*Cursor.visible = false;
            Cursor.lockState = CursorLockMode.None;*/

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
            InitCameraSystem(spider);
            InitStartGameScene(spider);
            InitBatteryProducts(spider);
        }

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

        private void GoToCheckPoints() =>
            _checkPointService.GoToNextPoint();
    }
}