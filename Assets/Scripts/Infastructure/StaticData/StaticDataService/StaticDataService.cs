using Infastructure.Common;
using Infastructure.StaticData.HUD;
using Infastructure.StaticData.Materials;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.VolumeProfiles;
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

        public void LoadStaticData()
        {
            GameStaticData = Resources.Load<GameStaticData>(AssetsPath.GameDataPath);

            SpiderStaticData = Resources.Load<SpiderStaticData>(AssetsPath.SpiderDataPath);
            HudStaticData = Resources.Load<HudStaticData>(AssetsPath.HudDataPath);
            MaterialsStaticData = Resources.Load<MaterialsStaticData>(AssetsPath.MaterialsDataPath);
            VolumeProfilesStaticData = Resources.Load<VolumeProfilesStaticData>(AssetsPath.VolumeProfilesDataPath);
        }
    }
}