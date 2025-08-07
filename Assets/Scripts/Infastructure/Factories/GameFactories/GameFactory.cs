using CameraFollow;
using HUD;
using Infastructure.Common;
using Infastructure.StaticData.StaticDataService;
using SpiderController;
using UnityEngine;
using Zenject;

namespace Infastructure.Factories.GameFactories
{
    public class GameFactory : IGameFactory
    {
        private readonly DiContainer _diContainer;
        private readonly IStaticDataService _staticDataService;

        public GameFactory(DiContainer diContainer, IStaticDataService staticDataService)
        {
            _diContainer = diContainer;
            _staticDataService = staticDataService;
        }

        public GameObject CreateSpider()
        {
            Spider spider = _diContainer.InstantiatePrefabResourceForComponent<Spider>(AssetsPath.SpiderPath);
            spider.Initialize();

            SpiderUI spiderUI = spider.GetComponent<SpiderUI>();
            spiderUI.Initialize();

            return spider.gameObject;
        }

        public void CreateCameraSystem(Transform spiderTransform)
        {
            CameraSystem cameraSystem =
                _diContainer.InstantiatePrefabResourceForComponent<CameraSystem>(AssetsPath.CameraSystemPath);
            cameraSystem.Initialize(spiderTransform);
        }

        public void CreateHUD()
        {
            Vector3 finishTargetPosition = _staticDataService.GameStaticData.FinishTargetPosition;
            RectTransform arrowUIPrefab = _staticDataService.HudStaticData.ArrowUIPrefab;

            HudUI hud = _diContainer.InstantiatePrefabResourceForComponent<HudUI>(AssetsPath.HUDPath);
            RectTransform arrowUI = Object.Instantiate(arrowUIPrefab, hud.transform);

            hud.Initialize(finishTargetPosition, arrowUI);
        }
    }
}