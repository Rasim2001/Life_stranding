using System;

namespace Infastructure.Data
{
    [Serializable]
    public class PlayerProgress
    {
        public WorldProgressData WorldProgressData;

        public PlayerProgress()
        {
            WorldProgressData = new WorldProgressData();
        }
    }
}