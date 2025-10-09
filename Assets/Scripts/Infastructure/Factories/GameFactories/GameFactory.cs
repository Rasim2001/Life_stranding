using CameraFollow;
using GameDevBuddies;
using HUD;
using Infastructure.Common;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.XRay;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.XRay;
using PickupObjects;
using SpiderController;
using SpiderController.UI.Health;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.SceneManagement;
using Zenject;


namespace Infastructure.Factories.GameFactories
{
    public class GameFactory : IGameFactory
    {
        private readonly DiContainer _diContainer;
        private readonly IStaticDataService _staticDataService;
        private readonly ICheckPointService _checkPointService;
        private readonly IXRayService _xRayService;

        private string ActiveSceneName => SceneManager.GetActiveScene().name;

        public GameFactory(DiContainer diContainer, IStaticDataService staticDataService,
            ICheckPointService checkPointService, IXRayService xRayService)
        {
            _diContainer = diContainer;
            _staticDataService = staticDataService;
            _checkPointService = checkPointService;
            _xRayService = xRayService;
        }

        public Spider CreateSpider(Flower flower)
        {
            Vector3 spiderSpawnPosition =
                _staticDataService.GameStaticData.GameDatas[ActiveSceneName].SpiderSpawnPosition;
            Spider spider = _diContainer.InstantiatePrefabResourceForComponent<Spider>(AssetsPath.SpiderPath,
                spiderSpawnPosition, Quaternion.identity, null);
            spider.Initialize(flower);

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

        public HudUI CreateHUD(Flower flower, Spider spider)
        {
            ArrowUI arrowUIPrefab = _staticDataService.HudStaticData.ArrowUIPrefab;

            HudUI hud = _diContainer.InstantiatePrefabResourceForComponent<HudUI>(AssetsPath.HUDPath);
            hud.Initialize(arrowUIPrefab);

            hud.RegisterFlowerPoint(flower);
            hud.RegisterFinishTarget(_checkPointService.PointIndicator);

            SphereCollider sphereCollider = spider.RotationPlaneTransform.GetComponent<SphereCollider>();

            flower.Initialize(hud.FlowerPointIndicator);
            flower.Initialize(spider.RotationPlaneTransform, spider.BoundPlaneMeshRender, sphereCollider);
            flower.StopSimulatePhysics();

            _xRayService.Initialize(hud.XRayCollectionContainer);

            return hud;
        }

        public void CreateCheckPointIndicator()
        {
            TargetPointIndicatorMarker indicatorMarker =
                _diContainer.InstantiatePrefabResourceForComponent<TargetPointIndicatorMarker>(
                    AssetsPath.PointIndicatorPath);

            _checkPointService.PointIndicator = indicatorMarker.transform;
        }

        public Flower CreateFlower() =>
            _diContainer.InstantiatePrefabResourceForComponent<Flower>(AssetsPath.FlowerPath);

        public void CreateAllBatteryProducts(Spider spider)
        {
            foreach (Vector3 position in _staticDataService.GameStaticData.GameDatas[ActiveSceneName].BatteriesPoints)
            {
                BatteryProduct batteryProduct = _diContainer.InstantiatePrefabResourceForComponent<BatteryProduct>(
                    AssetsPath.BatteryProductPath,
                    position, Quaternion.identity,
                    null);

                XRayMarker xRayMarker = batteryProduct.GetComponent<XRayMarker>();
                _xRayService.Add(xRayMarker);

                SphereCollider sphereCollider = spider.RotationPlaneTransform.GetComponent<SphereCollider>();

                batteryProduct.Initialize(spider.RotationPlaneTransform, spider.BoundPlaneMeshRender, sphereCollider);
            }
        }


        public void CreateStartGameCutSceneTimeline(Spider spiderTransform)
        {
            /*GameObject cutScene = _diContainer.InstantiatePrefabResource(AssetsPath.StartGameCutSceneTimelinePath);
            StartGameCutSceneRunner startGameCutSceneRunner = cutScene.GetComponent<StartGameCutSceneRunner>();
            startGameCutSceneRunner.Initialize(spiderTransform.transform);*/
        }

        public void CreateTerrainScan(Spider spider)
        {
            Transform cameraTransform = Camera.main.transform;

            GameObject terrainScanObject = _diContainer.InstantiatePrefabResource(AssetsPath.TerrainScanPath);

            TerrainScanOriginPositioner terrainScanOriginPositioner =
                terrainScanObject.GetComponentInChildren<TerrainScanOriginPositioner>();
            terrainScanOriginPositioner.Initialize(cameraTransform, spider.transform);

            TerrainScanIconsRenderer terrainScanIconsRenderer =
                terrainScanObject.GetComponentInChildren<TerrainScanIconsRenderer>();
            terrainScanIconsRenderer.Initialize(cameraTransform);
        }
    }
}