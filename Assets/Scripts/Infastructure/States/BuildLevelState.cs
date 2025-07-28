using System;
using CameraFollow;
using DG.Tweening;
using Infastructure.Factories.GameFactories;
using Infastructure.Services.PlayerProgressService;
using Infastructure.StaticData.StaticDataService;
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

        public BuildLevelState(
            IGameFactory gameFactory,
            IStaticDataService staticData,
            IGameUIFactory uiFactory,
            IPersistentProgressService progressService
        )
        {
            _gameFactory = gameFactory;
            _staticData = staticData;
            _uiFactory = uiFactory;
            _progressService = progressService;
        }

        public void Initialize() =>
            InitGameWorld();


        public void Dispose()
        {
        }


        private void InitGameWorld()
        {
            Spider spider = InitSpider();
            InitCameraSystem(spider);
        }

        private Spider InitSpider()
        {
            Spider spider = _gameFactory.CreateSpider().GetComponent<Spider>();
            spider.Initialize();

            return spider;
        }

        private void InitCameraSystem(Spider spider)
        {
            CameraSystem cameraSystem = _gameFactory.CreateCameraSystem().GetComponent<CameraSystem>();
            cameraSystem.Initialize(spider.transform);
        }
    }
}