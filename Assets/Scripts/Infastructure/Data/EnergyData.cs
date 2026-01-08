using System;

namespace Infastructure.Data
{
    [Serializable]
    public class EnergyData
    {
        public string UniqueId;

        public EnergyData(string uniqueId) =>
            UniqueId = uniqueId;
    }
}