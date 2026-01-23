using UnityEngine;

namespace Infastructure.StaticData.GravityGun
{
    [CreateAssetMenu(fileName = "GravityGunData", menuName = "StaticData/GravityGunData")]
    public class GravityGunStaticData : ScriptableObject
    {
        public LayerMask GrabTargetLayer;

        public float GrabDistance = 2;
        public float GrabForce = 2;
        public float MaxGrabVelocity = 8;
    }
}