using System;
using System.Collections.Generic;

namespace Infastructure.Data
{
    [Serializable]
    public class WorldProgressData
    {
        public WaterData WaterData = new();
        public FlowerData FlowerData = new();
        public SpiderData SpiderData = new();
        public List<CheckpointData> CheckpointDatas = new();
        public List<BatteryProductData> BatteryProductDatas = new();
        public List<GeneratorData> GeneratorDatas = new();
        public List<EnergyData> EnergyDatas = new();
    }
}