using System;

namespace Infastructure.Data
{
    [Serializable]
    public class PlayerProgress
    {
        public int Version;
        public const int CurrentVersion = 1;

        public WorldProgressData WorldProgressData = new();
        public TaskPopupData TaskPopupData = new();
        public AbilityData AbilityData = new();
    }
}