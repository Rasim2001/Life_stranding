using System;

namespace Infastructure.Data
{
    [Serializable]
    public class WaterData
    {
        public Vector3Data WaterPosition;
        public bool IsStartingMove;
    }
}