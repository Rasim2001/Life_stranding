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
        public float JerkSpeed = 5;
        public AnimationCurve JerkCurve;
        public float JerkDuration = 1;

        [Header("RotationPlane")]
        public float MouseSensitivity = 2f;
        public float MaxAngle = 45f;
        public float RotationSpeed;

        [Header("MouseLookZoom")]
        public float SmoothTime = 0.3f;
        public float MouseRotationSpeed = 6f;
        public float MouseSpeed = 400f;
        public float ScrollSensitivity = 15f;

        [Header("AirbornState")]
        public float FallSpeed;
        public float FallWithoutEnergySpeed;

        public float MaxHeight;
        public float TimeToReachMaxHeight;
        public float StartYVelocity => 2 * MaxHeight / TimeToReachMaxHeight;
        public float BaseGravity => 2f * MaxHeight / (TimeToReachMaxHeight * TimeToReachMaxHeight);


        [Header("UI")]
        public float EnergyFillAmount;
        public float EnergyFillSpeed;
        public float EnergySpendAirbornSpeed;
        public float EnergySpendFastRunningSpeed;
        public float EnergySpendJerkingSpeed;
        public float EnergySpendFreezingFlowerSpeed;
    }
}