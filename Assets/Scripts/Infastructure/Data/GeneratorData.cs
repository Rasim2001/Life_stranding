using System;

namespace Infastructure.Data
{
    [Serializable]
    public class GeneratorData
    {
        public string UniqueId;
        public bool IsLaunched;

        public GeneratorData(string uniqueId, bool isLaunched)
        {
            UniqueId = uniqueId;
            IsLaunched = isLaunched;
        }
    }
}