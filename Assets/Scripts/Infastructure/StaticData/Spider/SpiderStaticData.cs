using UnityEngine;

namespace Infastructure.StaticData.Spider
{
    [CreateAssetMenu(fileName = "SpiderData", menuName = "StaticData/SpiderData")]
    public class SpiderStaticData : ScriptableObject
    {
        public LayerMask LayerMask;

        [Header("SpiderMove")]
        public float StepLength = 0.85f;
        public float Speed = 3;
        public float LerpForwardSpeed = 60;
        public float DistanceFromGround = 0.5f;
        public float LerpSpeedFromGround = 10;
        public float FastSpeed = 6;

        [Header("RotationPlane")]
        public float MouseSensitivity = 2f;
        public float MaxAngle = 45f;
        public float RotationSpeed;
    }
}