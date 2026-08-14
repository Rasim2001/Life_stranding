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
        // Absolute orbit angle where the fourth anchor (Steep) sits for all three interpolated
        // params below — between Middle and the upper limit (MaxPitchDownAngle), same frame as that
        // limit. Own anchor because one point for the whole upper half couldn't frame both a ~45°
        // overhead look and the near-vertical 84° one distinctly — needed a knee, not just a slope.
        // Clamped internally to stay at or above Middle and at or below Top, so a bad value shortens
        // a segment instead of inverting the spline.
        public float CameraSteepAngle = 45f;

        // Height above the spider the camera aims at, along the spider's own up. Puts the cargo
        // in frame instead of the spider's own origin. Interpolated across the orbit through four
        // anchors: Bottom at the lower orbit limit (looking up), Middle at the neutral angle
        // (standard third-person — this is the one the zero-pitch invariant protects), Steep at
        // CameraSteepAngle, Top at the upper orbit limit (looking down).
        // See CameraPitchMath.InterpolateByAngle.
        public float CameraAimHeightBottom = 0f;
        public float CameraAimHeightMiddle = 0f;
        public float CameraAimHeightSteep = 0f;
        public float CameraAimHeightTop = 0f;
        // How far ahead of the spider the camera aims, same four orbit anchors. The complement of
        // the heights above, because the two axes trade places as the camera swings: at the lower
        // limit up is perpendicular to the view and forward lies along it, at 84° it is the other
        // way round. Measured share of the frame for a value of 0.85 — height: 0.168 bottom /
        // 0.023 top, forward: 0.000 bottom / 0.163 top. So Top is the one that frames the
        // overhead shot and Bottom there does nothing.
        public float CameraAimForwardBottom = 0f;
        public float CameraAimForwardMiddle = 0f;
        public float CameraAimForwardSteep = 0f;
        public float CameraAimForwardTop = 0f;
        // Zoom: multiplies the orbit radius the artist authored in the prefab (currently 3.606),
        // so 1 = exactly as authored, below 1 = closer, above 1 = further. A scale rather than an
        // absolute distance so the neutral anchor at 1 still reproduces the authored ShoulderOffset
        // and re-authoring the prefab does not invalidate these numbers.
        // Trade-off worth knowing: constant radius was the whole point of the orbit rework — it is
        // what stopped the spider shrinking when the camera swung overhead. Moving these away from
        // each other brings that size change back, deliberately.
        // Note it also rescales the framing: the aim offsets above shift the frame by
        // atan(offset / distance), so a smaller radius makes the same aim value bite harder.
        public float CameraOrbitRadiusScaleBottom = 1f;
        public float CameraOrbitRadiusScaleMiddle = 1f;
        public float CameraOrbitRadiusScaleSteep = 1f;
        public float CameraOrbitRadiusScaleTop = 1f;
        // Shifts the middle anchor itself, in degrees off the pose authored in the prefab's
        // ShoulderOffset — negative lowers the camera, positive raises it. Lives here rather than in
        // the prefab because the prefab value is read once at Initialize and cannot be dialled in
        // during play, and because moving it there would also move the base radius the scales above
        // multiply. 0 reproduces the authored pose exactly, so the zero-regression invariant holds.
        // Height at the middle = radius * sin(authoredAngle + this).
        public float CameraNeutralAngleOffset = 0f;

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