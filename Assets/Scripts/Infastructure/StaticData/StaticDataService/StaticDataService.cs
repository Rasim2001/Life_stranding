using Infastructure.Common;
using Infastructure.StaticData.GlobalWater;
using Infastructure.StaticData.HUD;
using Infastructure.StaticData.Materials;
using Infastructure.StaticData.Product;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.Stikers;
using Infastructure.StaticData.Task;
using Infastructure.StaticData.VolumeProfiles;
using Infastructure.StaticData.Windows;
using Infastructure.StaticData.XRay;
using UnityEngine;

namespace Infastructure.StaticData.StaticDataService
{
    public class StaticDataService : IStaticDataService
    {
        public SpiderStaticData SpiderStaticData { get; private set; }
        public HudStaticData HudStaticData { get; private set; }
        public GameStaticData GameStaticData { get; private set; }
        public MaterialsStaticData MaterialsStaticData { get; private set; }
        public VolumeProfilesStaticData VolumeProfilesStaticData { get; private set; }
        public XRayCollectionStaticData XRayCollectionStaticData { get; private set; }
        public ProductsStaticData ProductsStaticData { get; private set; }
        public StickersStaticData StickersStaticData { get; private set; }
        public TasksStaticData TasksStaticData { get; private set; }
        public WaterStaticData WaterStaticData { get; private set; }
        public WindowsLocalizationStaticData WindowsLocalizationStaticData { get; private set; }

        public void LoadStaticData()
        {
            GameStaticData = Resources.Load<GameStaticData>(AssetsPath.GameDataPath);

            SpiderStaticData = Resources.Load<SpiderStaticData>(AssetsPath.SpiderDataPath);
            HudStaticData = Resources.Load<HudStaticData>(AssetsPath.HudDataPath);
            MaterialsStaticData = Resources.Load<MaterialsStaticData>(AssetsPath.MaterialsDataPath);
            VolumeProfilesStaticData = Resources.Load<VolumeProfilesStaticData>(AssetsPath.VolumeProfilesDataPath);
            XRayCollectionStaticData = Resources.Load<XRayCollectionStaticData>(AssetsPath.XRayCollectionDataPath);
            ProductsStaticData = Resources.Load<ProductsStaticData>(AssetsPath.ProductStaticDataPath);
            StickersStaticData = Resources.Load<StickersStaticData>(AssetsPath.StickersStaticDataPath);
            TasksStaticData = Resources.Load<TasksStaticData>(AssetsPath.TasksStaticDataPath);
            WaterStaticData = Resources.Load<WaterStaticData>(AssetsPath.WaterStaticDataPath);
            WindowsLocalizationStaticData =
                Resources.Load<WindowsLocalizationStaticData>(AssetsPath.WindowsLocalizationStaticDataPath);
        }
    }
}