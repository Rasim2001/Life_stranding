using CameraFollow;
using HUD;
using Infastructure.Common;
using Infastructure.CutScene;
using Infastructure.Services.CheckPoint;
using Infastructure.StaticData.StaticDataService;
using SpiderController;
using SpiderController.UI.Health;
using UnityEngine;
using Zenject;


namespace Infastructure.Factories.GameFactories
{
    public class GameFactory : IGameFactory
    {
        private readonly DiContainer _diContainer;
        private readonly IStaticDataService _staticDataService;
        private readonly ICheckPointService _checkPointService;

        public GameFactory(DiContainer diContainer, IStaticDataService staticDataService,
            ICheckPointService checkPointService)
        {
            _diContainer = diContainer;
            _staticDataService = staticDataService;
            _checkPointService = checkPointService;
        }

        public Spider CreateSpider(HudUI hudUI)
        {
            Spider spider = _diContainer.InstantiatePrefabResourceForComponent<Spider>(AssetsPath.SpiderPath);
            spider.Initialize(hudUI);

            SpiderUI spiderUI = spider.GetComponent<SpiderUI>();
            spiderUI.Initialize();

            return spider;
        }

        public void CreateCameraSystem(Spider spiderTransform)
        {
            CameraSystem cameraSystem =
                _diContainer.InstantiatePrefabResourceForComponent<CameraSystem>(AssetsPath.CameraSystemPath);
            cameraSystem.Initialize(spiderTransform);
        }

        public HudUI CreateHUD()
        {
            RectTransform arrowUIPrefab = _staticDataService.HudStaticData.ArrowUIPrefab;

            HudUI hud = _diContainer.InstantiatePrefabResourceForComponent<HudUI>(AssetsPath.HUDPath);
            hud.Initialize(hud.transform, arrowUIPrefab);

            return hud;
        }

        public void CreateCheckPointIndicator()
        {
            TargetPointIndicatorMarker indicatorMarker =
                _diContainer.InstantiatePrefabResourceForComponent<TargetPointIndicatorMarker>(
                    AssetsPath.PointIndicatorPath);

            _checkPointService.PointIndicator = indicatorMarker.transform;
        }

        public void CreateStartGameCutSceneTimeline(Spider spiderTransform)
        {
            GameObject cutScene = _diContainer.InstantiatePrefabResource(AssetsPath.StartGameCutSceneTimelinePath);
            StartGameCutSceneRunner startGameCutSceneRunner = cutScene.GetComponent<StartGameCutSceneRunner>();
            startGameCutSceneRunner.Initialize(spiderTransform.transform);
        }
    }
}