using System;

namespace Infastructure.Data
{
    [Serializable]
    public class PlayerProgress
    {
        public WorldProgressData WorldProgressData = new();
        public TaskPopupData TaskPopupData = new();
        public AbilityData AbilityData = new();
    }
}