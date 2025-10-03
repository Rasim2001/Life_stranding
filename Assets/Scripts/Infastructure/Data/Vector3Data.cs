using System;

namespace Infastructure.Data
{
    [Serializable]
    public class Vector2Data
    {
        public float X, Y;

        public Vector2Data(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}