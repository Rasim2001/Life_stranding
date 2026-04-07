using UnityEngine;

namespace Infastructure.Data
{
    public static class DataExtensions
    {
        public static Vector3Data AsVectorData(this Vector3 vector) =>
            new(vector.x, vector.y, vector.z);

        public static Vector3 AsUnityVector(this Vector3Data vector3Data) =>
            new(vector3Data.X, vector3Data.Y, vector3Data.Z);
    }
}