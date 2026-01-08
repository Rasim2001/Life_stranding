using System;

namespace Infastructure.Data
{
    [Serializable]
    public class CheckpointData
    {
        public bool IsReady;
        public string UniqueId;

        public CheckpointData(bool isReady, string uniqueId)
        {
            IsReady = isReady;
            UniqueId = uniqueId;
        }
    }
}