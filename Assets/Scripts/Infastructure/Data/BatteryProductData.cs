using System;

namespace Infastructure.Data
{
    [Serializable]
    public class BatteryProductData
    {
        public Vector3Data Position;
        public Vector3Data Rotation;
        public string UniqueId;
        public bool IsPuttingDown;

        public BatteryProductData(Vector3Data position, Vector3Data rotation, string uniqueId, bool isPuttingDown)
        {
            Position = position;
            Rotation = rotation;
            UniqueId = uniqueId;
            IsPuttingDown = isPuttingDown;
        }
    }
}