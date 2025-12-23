using System.Collections.Generic;
using CameraFollow;
using GameDevBuddies;
using HUD;
using Infastructure.Common;
using Infastructure.CutScene;
using Infastructure.CutScene.Custom.Receivers;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.SpiderTrack;
using Infastructure.Services.XRay;
using Infastructure.StaticData;
using Infastructure.StaticData.Product;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.XRay;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using PickupObjects.Skills;
using SpiderController;
using SpiderController.UI.Health;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;


namespace Infastructure.Factories.GameFactories
{
    public class GameFactory : IGameFactory
    {
        private readonly DiContainer _diContainer;
        private readonly IStaticDataService _staticDataService;
        private readonly IBiospherePointService _biospherePointService;
        private readonly IXRayService _xRayService;
        private readonly ISpiderTrackService _spiderTrackService;
        private readonly ICameraProviderService _cameraProviderService;

        private string ActiveSceneName => SceneManager.GetActiveScene().name;

        public GameFactory(DiContainer diContainer, IStaticDataService staticDataService,
            IBiospherePointService biospherePointService, IXRayService xRayService,
            ISpiderTrackService spiderTrackService, ICameraProviderService cameraProviderService)
        {
            _spiderTrackService = spiderTrackService;
            _cameraProviderService = cameraProviderService;
            _diContainer = diContainer;
            _staticDataService = staticDataService;
            _biospherePointService = biospherePointService;
            _xRayService = xRayService;
        }

        public Spider CreateSpider(Flower flower)
        {
            WorldData worldData =
                _staticDataService.GameStaticData.GameDatas[ActiveSceneName].SpiderSpawnData;
            Spider spider = _diContainer.InstantiatePrefabResourceForComponent<Spider>(AssetsPath.SpiderPath,
                worldData.WorldPosition, worldData.WorldRotation, null);
            spider.Initialize(flower);

            _spiderTrackService.Spider = spider;
            _spiderTrackService.Flower = flower;

            return spider;
        }

        public void CreateCameraSystem(Spider spider)
        {
            CameraSystem cameraSystem =
                _diContainer.InstantiatePrefabResourceForComponent<CameraSystem>(AssetsPath.CameraSystemPath);
            cameraSystem.Initialize(spider);
        }

        public HudUI CreateHUD(Flower flower, Spider spider)
        {
            ArrowUI arrowUIPrefab = _staticDataService.HudStaticData.ArrowUIPrefab;

            HudUI hud = _diContainer.InstantiatePrefabResourceForComponent<HudUI>(AssetsPath.HUDPath);
            hud.Initialize(arrowUIPrefab);

            hud.RegisterFlowerPoint(flower);
            hud.RegisterFinishTarget(_biospherePointService.PointIndicator);

            flower.Initialize(hud.FlowerPointIndicator, spider.StateMachineData);
            flower.Initialize(spider.RotationPlaneTransform, spider.PlatformSelector);

            XRayMarker xRayMarker = flower.GetComponent<XRayMarker>();
            xRayMarker.Type = ProductType.Flower; //TODO:

            _xRayService.Add(xRayMarker);

            _xRayService.Initialize(hud.XRayCollectionContainer, hud.transform, hud.DisabledContainer);

            return hud;
        }

        public void CreateCheckPoints()
        {
            List<WorldData> checkPoints = _staticDataService.GameStaticData.GameDatas[ActiveSceneName].CheckPoints;

            for (int i = 0; i < checkPoints.Count; i++)
            {
                if (IsBiospherePoint(i, checkPoints))
                {
                    TargetPointIndicatorMarker indicatorMarker =
                        _diContainer.InstantiatePrefabResourceForComponent<TargetPointIndicatorMarker>(
                            AssetsPath.BiospherePointIndicatorPath, checkPoints[i].WorldPosition,
                            checkPoints[i].WorldRotation,
                            null);

                    _biospherePointService.PointIndicator = indicatorMarker.transform;
                }
                else
                {
                    _diContainer.InstantiatePrefabResource(AssetsPath.CheckPointPath,
                        checkPoints[i].WorldPosition, checkPoints[i].WorldRotation, null);
                }
            }
        }

        public Flower CreateFlower()
        {
            ProductType productType = ProductType.Flower;

            ProductsStaticData productsStaticData = _staticDataService.ProductsStaticData;
            GameObject prefab = productsStaticData.ProductsDictionary[productType].Prefab;

            WorldData worldData =
                _staticDataService.GameStaticData.GameDatas[ActiveSceneName].FlowerSpawnData;

            Flower flower = _diContainer.InstantiatePrefabForComponent<Flower>(prefab, worldData.WorldPosition,
                worldData.WorldRotation, null);

            IProduct product = flower.GetComponent<IProduct>();
            product.ProductType = productType;

            return flower;
        }

        public void CreateAllBatteryProducts(Spider spider)
        {
            ProductType productType = ProductType.Battery;

            ProductsStaticData productsStaticData = _staticDataService.ProductsStaticData;
            GameObject prefab = productsStaticData.ProductsDictionary[productType].Prefab;

            foreach (WorldData worldData in _staticDataService.GameStaticData.GameDatas[ActiveSceneName]
                         .BatteriesPoints)
            {
                BatteryProduct batteryProduct =
                    _diContainer.InstantiatePrefabForComponent<BatteryProduct>(prefab, worldData.WorldPosition,
                        worldData.WorldRotation, null);

                IProduct product = batteryProduct.GetComponent<IProduct>();
                product.ProductType = productType;

                XRayMarker xRayMarker = batteryProduct.GetComponent<XRayMarker>();
                xRayMarker.Type = productType;

                _xRayService.Add(xRayMarker);

                batteryProduct.Initialize(spider.RotationPlaneTransform, spider.PlatformSelector);
                batteryProduct.Initialize(spider.StateMachineData);
            }
        }

        public void CreateEnergyProducts()
        {
            ProductType productType = ProductType.Energy;

            ProductsStaticData productsStaticData = _staticDataService.ProductsStaticData;
            GameObject prefab = productsStaticData.ProductsDictionary[productType].Prefab;

            foreach (WorldData data in _staticDataService.GameStaticData.GameDatas[ActiveSceneName].EnergyPoints)
            {
                EnergyProduct energyProduct =
                    _diContainer.InstantiatePrefabForComponent<EnergyProduct>(prefab, data.WorldPosition,
                        data.WorldRotation,
                        null);

                IProduct product = energyProduct.GetComponent<IProduct>();
                product.ProductType = productType;

                XRayMarker xRayMarker = energyProduct.GetComponent<XRayMarker>();
                xRayMarker.Type = productType;

                _xRayService.Add(xRayMarker);
            }
        }

        public void CreateElephantProduct(Spider spider)
        {
            ProductType productType = ProductType.Elephant;

            ProductsStaticData productsStaticData = _staticDataService.ProductsStaticData;
            GameObject prefab = productsStaticData.ProductsDictionary[productType].Prefab;

            foreach (WorldData data in _staticDataService.GameStaticData.GameDatas[ActiveSceneName].ElephantPoints)
            {
                ElephantProduct elephantProduct =
                    _diContainer.InstantiatePrefabForComponent<ElephantProduct>(prefab, data.WorldPosition,
                        data.WorldRotation,
                        null);

                IProduct product = elephantProduct.GetComponent<IProduct>();
                product.ProductType = productType;

                elephantProduct.Initialize(spider.RotationPlaneTransform, spider.PlatformSelector);
                elephantProduct.Initialize(spider.StateMachineData);
            }
        }

        public void CreateSkillProducts()
        {
            List<ProductSkillData> productSkillDatas =
                _staticDataService.GameStaticData.GameDatas[ActiveSceneName].SkillsData;

            foreach (ProductSkillData skillData in productSkillDatas)
            {
                ProductType productType = skillData.ProductType;

                ProductsStaticData productsStaticData = _staticDataService.ProductsStaticData;
                GameObject prefab = productsStaticData.ProductsDictionary[productType].Prefab;

                SkillProduct skillProduct = _diContainer.InstantiatePrefabForComponent<SkillProduct>(prefab,
                    skillData.WorldPosition,
                    skillData.WorldRotation,
                    null);

                IProduct product = skillProduct.GetComponent<IProduct>();
                product.ProductType = productType;

                XRayMarker xRayMarker = skillProduct.GetComponent<XRayMarker>();
                xRayMarker.Type = productType;

                _xRayService.Add(xRayMarker);
            }
        }

        public void CreateAllGenerators()
        {
            List<WorldData> generatorPoints =
                _staticDataService.GameStaticData.GameDatas[ActiveSceneName].GeneratorPoints;

            foreach (WorldData worldData in generatorPoints)
            {
                _diContainer.InstantiatePrefabResource(AssetsPath.GeneratorPath,
                    worldData.WorldPosition, worldData.WorldRotation, null);
            }
        }


        public void CreateStartGameCutSceneTimeline(Spider spider)
        {
            StartGameCutSceneRunner cutScene =
                _diContainer.InstantiatePrefabResourceForComponent<StartGameCutSceneRunner>(AssetsPath
                    .StartGameCutSceneTimelinePath);

            PlayDirectorMarkerReceiver playDirectorMarkerReceiver = cutScene.GetComponent<PlayDirectorMarkerReceiver>();
            playDirectorMarkerReceiver.Initialize(spider);

            cutScene.Initialize(spider);
        }

        public void CreateTerrainScan(Spider spider)
        {
            GameObject terrainScanObject = _diContainer.InstantiatePrefabResource(AssetsPath.TerrainScanPath);

            TerrainScanOriginPositioner terrainScanOriginPositioner =
                terrainScanObject.GetComponentInChildren<TerrainScanOriginPositioner>();
            terrainScanOriginPositioner.Initialize(_cameraProviderService.CameraTransform, spider.transform);

            TerrainScanIconsRenderer terrainScanIconsRenderer =
                terrainScanObject.GetComponentInChildren<TerrainScanIconsRenderer>();
            terrainScanIconsRenderer.Initialize(_cameraProviderService.CameraTransform);
        }

        private bool IsBiospherePoint(int i, List<WorldData> checkPoints) =>
            i == checkPoints.Count - 1;
    }
}