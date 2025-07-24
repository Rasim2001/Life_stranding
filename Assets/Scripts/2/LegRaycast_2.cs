using _1;
using UnityEngine;

namespace _2
{
    public class LegRaycast_2 : LegRaycast
    {
        protected override Vector3 GetDirection() =>
            -transform.up;
    }
}