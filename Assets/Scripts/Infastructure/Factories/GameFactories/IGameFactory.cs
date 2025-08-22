using HUD;
using SpiderController;
using UnityEngine;

namespace Infastructure.Factories.GameFactories
{
    public interface IGameFactory
    {
        Spider CreateSpider(HudUI hudUI);
        void CreateCameraSystem(Spider spider);
        HudUI CreateHUD();
        void CreateCheckPointIndicator();
        void CreateStartGameCutSceneTimeline(Spider spiderTransform);
    }
}