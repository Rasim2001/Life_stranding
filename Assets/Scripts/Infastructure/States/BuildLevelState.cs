using System;
using CameraFollow;
using DG.Tweening;
using Infastructure.Common;
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
        private readonly ISceneLoader _sceneLoader;

        public BuildLevelState(
            IGameFactory gameFactory,
            IStaticDataService staticData,
            IGameUIFactory uiFactory,
            IPersistentProgressService progressService,
            ISceneLoader sceneLoader
        )
        {
            _gameFactory = gameFactory;
            _staticData = staticData;
            _uiFactory = uiFactory;
            _progressService = progressService;
            _sceneLoader = sceneLoader;
        }

        public void Initialize()
        {
            if (!_sceneLoader.IsGameScene())
                return;

            InitGameWorld();
        }


        public void Dispose()
        {
        }


        private void InitGameWorld()
        {
            GameObject spider = InitSpider();

            InitCameraSystem(spider.transform);
            InitHUD();
        }


        private GameObject InitSpider() =>
            _gameFactory.CreateSpider();

        private void InitCameraSystem(Transform spiderTransform) =>
            _gameFactory.CreateCameraSystem(spiderTransform);

        private void InitHUD() =>
            _gameFactory.CreateHUD();
    }
}