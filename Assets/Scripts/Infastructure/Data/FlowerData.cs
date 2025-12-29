using System;

namespace Infastructure.Data
{
    [Serializable]
    public class FlowerData
    {
        public Vector3Data Position;
        public Vector3Data Rotation;
        public bool IsPuttingDown;
        public bool IsOnPlatform;
    }
}