using Infastructure.StaticData.GlobalWater;
using Infastructure.StaticData.HUD;
using Infastructure.StaticData.LastChance;
using Infastructure.StaticData.Materials;
using Infastructure.StaticData.Product;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.Stikers;
using Infastructure.StaticData.Task;
using Infastructure.StaticData.VolumeProfiles;
using Infastructure.StaticData.Windows;
using Infastructure.StaticData.XRay;

namespace Infastructure.StaticData.StaticDataService
{
    public interface IStaticDataService
    {
        void LoadStaticData();
        SpiderStaticData SpiderStaticData { get; }
        HudStaticData HudStaticData { get; }
        GameStaticData GameStaticData { get; }
        MaterialsStaticData MaterialsStaticData { get; }
        VolumeProfilesStaticData VolumeProfilesStaticData { get; }
        XRayCollectionStaticData XRayCollectionStaticData { get; }
        ProductsStaticData ProductsStaticData { get; }
        StickersStaticData StickersStaticData { get; }
        TasksStaticData TasksStaticData { get; }
        WaterStaticData WaterStaticData { get; }
        WindowsLocalizationStaticData WindowsLocalizationStaticData { get; }
        LastChanceStaticData LastChanceStaticData { get; }
    }
}