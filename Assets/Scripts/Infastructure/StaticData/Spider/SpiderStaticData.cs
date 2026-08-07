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
        public float ScrollSensitivity = 15f;
        public float WorldUpSmoothRotation = 2;

        [Header("CameraPitch")]
        // Absolute angles from the horizontal behind the spider, not deltas from neutral — 90
        // would put the camera straight overhead. The authored shoulder offset sits at ~33.7°,
        // so that's where the player's pitch starts from within this range.
        public float MaxPitchDownAngle = 80f;
        public float MaxPitchUpAngle = 20f;
        // Degrees per unit of mouse delta. Not multiplied by deltaTime — mouse input is already a
        // per-frame delta, so scaling it by frame time would make sensitivity framerate-dependent
        // (matches how MouseRotationSpeedX is used).
        public float PitchSensitivity = 3f;
        public float PitchScreenOffset = 0f;
        // Height above the spider the camera aims at, along the spider's own up. Puts the cargo
        // in frame instead of the spider's own origin. Guess for the first pass — tune in the
        // inspector.
        public float CameraAimHeight = 1f;

        [Header("CameraHorizon")]
        // Горизонт камеры следует за верхом паука только на устойчивом крупном наклоне — стена,
        // потолок. Камень, ступенька и склон остаются в мировой горизонтали, иначе камеру
        // заваливает вслед за корпусом на каждой неровности.
        // Насколько верх паука должен разойтись с текущей целью горизонта, чтобы цель
        // переставилась. Ниже порога — камень, ступенька, склон: горизонт не трогаем.
        public float HorizonFollowEnterAngle = 55f;
        // Сколько порог должен держаться, чтобы сработать. Защита от резкой кромки, которая на
        // один кадр задирает паука за порог.
        public float HorizonFollowDwellTime = 0.15f;
        // Разворот считается законченным, когда верх паука перестал уходить дальше этого угла в
        // течение HorizonSettleTime. Цель фиксируется по итогу разворота, а не на полпути.
        public float HorizonSettleAngle = 3f;
        public float HorizonSettleTime = 0.25f;

        [Header("CameraClimb")]
        // Автоподъём камеры, когда паук перестаёт быть горизонтальным. Наклон, при котором подъём
        // достигает максимума: чем больше значение, тем позднее набирается эффект на пологих склонах.
        public float ClimbTiltMaxAngle = 70f;
        // Высота плеча при максимальном наклоне. Базовая высота берётся из префаба (сейчас 2),
        // так что разница между ними и есть весь ход подъёма.
        public float ClimbShoulderMaxY = 3.5f;

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