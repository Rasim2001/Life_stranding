using System;
using CameraFollow;
using DG.Tweening;
using HUD;
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

            /*Cursor.visible = false;
            Cursor.lockState = CursorLockMode.None;*/

            InitGameWorld();
        }


        public void Dispose()
        {
        }


        private void InitGameWorld()
        {
            HudUI hudUI = InitHUD();
            Spider spider = InitSpider(hudUI);

            InitCameraSystem(spider);
        }


        private Spider InitSpider(HudUI hudUI) =>
            _gameFactory.CreateSpider(hudUI);

        private void InitCameraSystem(Spider spiderTransform) =>
            _gameFactory.CreateCameraSystem(spiderTransform);

        private HudUI InitHUD() =>
            _gameFactory.CreateHUD();
    }
}