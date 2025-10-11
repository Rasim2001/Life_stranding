using HUD;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using SpiderController;
using UnityEngine;

namespace Infastructure.Factories.GameFactories
{
    public interface IGameFactory
    {
        Spider CreateSpider(Flower flower);
        void CreateCameraSystem(Spider spider);
        HudUI CreateHUD(Flower flower, Spider spider);
        void CreateCheckPointIndicator();
        void CreateStartGameCutSceneTimeline(Spider spiderTransform);
        Flower CreateFlower();
        void CreateAllBatteryProducts(Spider spider);
        void CreateTerrainScan(Spider spider);
        void CreateEnergyProducts();
    }
}