using UnityEngine;

namespace Cameras.SpiderCameras
{
    /// <summary>
    /// Pure pitch math, no Unity Transform/Cinemachine dependencies — kept testable
    /// once this project has a test assembly (it doesn't yet, see .scratch/camera-pitch/spec.md).
    /// </summary>
    public static class CameraPitchMath
    {
        public static float ClampPitch(float pitch, float maxDownAngle, float maxUpAngle) =>
            Mathf.Clamp(pitch, -maxUpAngle, maxDownAngle);

        /// <summary>
        /// Vertical orbit expressed as a shoulder offset, because CinemachineThirdPersonFollow
        /// applies ShoulderOffset in the heading frame — which is yaw only, pitch stripped
        /// (see GetHeading in CinemachineThirdPersonFollow). Rotating the follow target therefore
        /// does nothing to camera placement; the arc has to be built here instead.
        ///
        /// At pitch 0 this returns exactly (x, baseY, -radius), i.e. the authored offset, so zero
        /// pitch reproduces the original camera placement bit for bit.
        /// Positive pitch swings the camera up and closer, which makes it look down at the spider.
        /// </summary>
        public static Vector3 ShoulderArc(float x, float baseY, float radius, float pitchDegrees)
        {
            float rad = pitchDegrees * Mathf.Deg2Rad;
            return new Vector3(
                x,
                baseY + radius * Mathf.Sin(rad),
                -radius * Mathf.Cos(rad));
        }

        /// <summary>
        /// Linear framing offset: at zero pitch the composer keeps its authored screen position;
        /// at max downward pitch it's pushed by <paramref name="maxScreenOffset"/> so the spider
        /// keeps drifting toward the bottom third instead of staying centred.
        /// </summary>
        public static float FramingScreenOffset(float pitch, float maxDownAngle, float maxScreenOffset)
        {
            if (maxDownAngle <= 0f || pitch <= 0f)
                return 0f;

            float t = Mathf.Clamp01(pitch / maxDownAngle);
            return t * maxScreenOffset;
        }
    }
}
