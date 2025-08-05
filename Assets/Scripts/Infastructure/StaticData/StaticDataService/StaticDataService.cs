using Infastructure.Common;
using Infastructure.StaticData.Spider;
using UnityEngine;

namespace Infastructure.StaticData.StaticDataService
{
    public class StaticDataService : IStaticDataService
    {
        public SpiderStaticData SpiderStaticData { get; private set; }

        public void LoadStaticData() => 
            SpiderStaticData = Resources.Load<SpiderStaticData>(AssetsPath.SpiderDataPath);
    }
}