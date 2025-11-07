using HUD;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController;
using UnityEngine;

namespace Infastructure.Factories.GameFactories
{
    public interface IGameFactory
    {
        Spider CreateSpider(Flower flower);
        void CreateCameraSystem(Spider spider);
        HudUI CreateHUD(Flower flower, Spider spider);
        void CreateCheckPoints();
        void CreateStartGameCutSceneTimeline(Spider spiderTransform);
        Flower CreateFlower();
        void CreateAllBatteryProducts(Spider spider);
        void CreateTerrainScan(Spider spider);
        void CreateEnergyProducts();
        void CreateElephantProduct(Spider spider);
        void CreateSkillProducts();
        void CreateAllGenerators();
        void CreateGlobalWater();
    }
}