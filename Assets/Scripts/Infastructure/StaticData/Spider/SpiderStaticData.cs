using UnityEngine;

namespace Infastructure.StaticData.Spider
{
    [CreateAssetMenu(fileName = "SpiderData", menuName = "StaticData/SpiderData")]
    public class SpiderStaticData : ScriptableObject
    {
        [Header("SpiderMove")]
        public float StepLength = 0.85f;
        public float GroundStateRayDistance = 3;
        public float AirbornStateRayDistance = 1;

        public float Speed = 3;
        public float LerpForwardSpeed = 60;
        public float DistanceFromGround = 0.5f;
        public float SlowdownDistanceFromGround = 0.25f;
        public float LerpSpeedFromGround = 10;
        public float SlowdownSpeed = 2;
        public float FastSpeed = 6;
        public float JerkSpeed = 5;
        public float JerkDuration = 1;
        public AnimationCurve JerkCurve;

        [Header("RotationPlane")]
        public float PlaneSensitivity = 2f;
        public float MaxAngle = 45f;
        public float PlaneRotationSpeed;
        public AnimationCurve PlaneReturnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("MouseLook")]
        public float SmoothTime = 0.3f;
        public float CameraRotationSpeed = 6f;
        public float MouseRotationSpeedX = 6;
        public float MouseRotationSpeedY = 400f;
        public float ScrollSensitivity = 15f;
        public float WorldUpSmoothRotation = 2;

        [Header("CameraShake")]
        public float MinShakeDistance = 5;
        public float MaxShakeDistance = 20;
        public float MinForceShake = 0.5f;
        public float MaxForceShake = 3;

        [Header("AirbornState")]
        public float FallSpeed;
        public float FallWithoutEnergySpeed;
        public float CrossLerpSpeed;

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
        public float EnergyAimingSpeed;

        [Header("Health")]
        public float MaxHealth;
        public float DamageAmount;
    }
}