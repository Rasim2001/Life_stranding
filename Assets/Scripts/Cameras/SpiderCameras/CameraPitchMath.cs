using UnityEngine;

namespace Cameras.SpiderCameras
{
    /// <summary>
    /// Pure pitch math, no Unity Transform/Cinemachine dependencies — kept testable
    /// once this project has a test assembly (it doesn't yet, see .scratch/camera-pitch/spec.md).
    /// </summary>
    public static class CameraPitchMath
    {
        /// <summary>
        /// Distance from the spider to the camera, derived from the authored shoulder offset.
        /// The orbit is centred on the spider itself, so this is the radius of the whole arc and
        /// it never changes with pitch — which is the point: the spider must not shrink when the
        /// camera swings overhead.
        /// </summary>
        public static float OrbitRadius(float authoredY, float authoredZ) =>
            Mathf.Sqrt(authoredY * authoredY + authoredZ * authoredZ);

        /// <summary>
        /// Where on that circle the authored offset sits, in degrees above the horizontal behind
        /// the spider. Everything the player does is measured from here, so zero player pitch
        /// reproduces the authored placement exactly.
        /// </summary>
        public static float NeutralOrbitAngle(float authoredY, float authoredZ) =>
            Mathf.Atan2(authoredY, -authoredZ) * Mathf.Rad2Deg;

        /// <summary>
        /// Clamps the player's pitch delta so that the resulting orbit angle
        /// (<paramref name="neutralAngle"/> + delta) stays inside the configured limits.
        /// The limits are absolute angles from the horizontal behind the spider — 90 would be
        /// straight overhead — not deltas from neutral, per the spec.
        /// </summary>
        public static float ClampPitch(float pitch, float neutralAngle, float maxDownAngle, float maxUpAngle) =>
            Mathf.Clamp(pitch, -maxUpAngle - neutralAngle, maxDownAngle - neutralAngle);

        /// <summary>
        /// Vertical orbit expressed as a shoulder offset, because CinemachineThirdPersonFollow
        /// applies ShoulderOffset in the heading frame — which is yaw only, pitch stripped
        /// (see GetHeading in CinemachineThirdPersonFollow). Rotating the follow target therefore
        /// does nothing to camera placement; the arc has to be built here instead.
        ///
        /// The circle is centred on the spider, offset up by <paramref name="centerY"/> — which is
        /// zero at rest and only lifts when the climb compensation raises the whole orbit.
        /// Feeding the authored shoulder height in here instead is exactly the bug this replaces:
        /// it put the spider off-centre, so its distance swung from 2.12 to 4.98 across the range.
        ///
        /// <paramref name="orbitAngleDegrees"/> is the full angle from the horizontal behind the
        /// spider, not the player's input: at the neutral angle this returns the authored offset
        /// bit for bit. 90 puts the camera straight overhead looking down.
        /// </summary>
        public static Vector3 ShoulderArc(float x, float centerY, float radius, float orbitAngleDegrees)
        {
            float rad = orbitAngleDegrees * Mathf.Deg2Rad;
            return new Vector3(
                x,
                centerY + radius * Mathf.Sin(rad),
                -radius * Mathf.Cos(rad));
        }

        /// <summary>
        /// Linear framing offset: at zero pitch the composer keeps its authored screen position;
        /// at the bottom of the downward travel it's pushed by <paramref name="maxScreenOffset"/>
        /// so the spider keeps drifting toward the bottom third instead of staying centred.
        /// <paramref name="maxDownTravel"/> is the travel available below neutral, not the absolute
        /// limit — measuring against the absolute one would already offset the framing at neutral.
        /// </summary>
        public static float FramingScreenOffset(float pitch, float maxDownTravel, float maxScreenOffset)
        {
            if (maxDownTravel <= 0f || pitch <= 0f)
                return 0f;

            float t = Mathf.Clamp01(pitch / maxDownTravel);
            return t * maxScreenOffset;
        }
    }
}
