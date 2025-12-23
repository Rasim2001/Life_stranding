using System;
using System.Collections.Generic;
using Infastructure.Data;

namespace Infastructure.Data
{
    [Serializable]
    public class WorldProgressData
    {
        public WaterData WaterData = new();
        public List<BatteryProductData> BatteryProductDatas = new List<BatteryProductData>();
    }
}

[Serializable]
public class WaterData
{
    public Vector3Data WaterPosition;
}

[Serializable]
public class BatteryProductData
{
    public Vector3Data Position;
    public Vector3Data Rotation;
    public string UniqueId;

    public BatteryProductData(Vector3Data position, Vector3Data rotation, string uniqueId)
    {
        Position = position;
        Rotation = rotation;
        UniqueId = uniqueId;
    }
}