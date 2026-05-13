using System;

namespace Infastructure.Data
{
    [Serializable]
    public class CheckpointData
    {
        public bool WasPicked;
        public bool IsReady;
        public string UniqueId;

        public CheckpointData(bool isReady, string uniqueId, bool wasPicked)
        {
            IsReady = isReady;
            UniqueId = uniqueId;
            WasPicked = wasPicked;
        }
    }
}