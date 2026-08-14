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

        /// <summary>
        /// Monotone cubic Hermite spline through four authored points keyed to the absolute orbit
        /// angle (the same frame as MaxPitchDownAngle/MaxPitchUpAngle — 0 is level behind the
        /// spider, 90 is straight overhead): <paramref name="bottom"/> at the lower orbit limit,
        /// <paramref name="middle"/> at the neutral angle, <paramref name="steep"/> at a configured
        /// angle between middle and top, <paramref name="top"/> at the upper orbit limit. At exactly
        /// the middle angle this returns <paramref name="middle"/> bit for bit — same zero-regression
        /// guarantee as the rest of this file, since a keyframe's own angle always lands its Hermite
        /// parameter on an exact 0 or 1. Axis-neutral: drives the aim height, the forward aim offset
        /// and the orbit radius scale alike.
        ///
        /// Superseded the three-point version (bottom/middle/top only) when a fourth anchor was
        /// added for framing a ~45° overhead look distinctly from the 84° near-vertical one — one
        /// anchor for the whole upper half could not shape both. The spline math generalises to the
        /// interior points that gained a neighbour on each side rather than one: each interior
        /// keyframe's slope is the harmonic mean of its two adjacent segment slopes
        /// (Fritsch–Carlson), zero only where the segments disagree on direction — a genuine local
        /// extremum, not an artifact of where the anchor happens to sit. The two outer keyframes stay
        /// pinned to zero slope, where running out of pitch range makes the ease-out correct. Plain
        /// Lerp was rejected for the slope jump it leaves at every interior anchor, felt as a notch
        /// while the camera sweeps through (measured 0.0148/degree with tuning in use at the time,
        /// and the composer has no damping of its own — Damping is 0,0 — to smooth it away). Per-half
        /// SmoothStep was rejected the other way: it is zero-slope at *both* its ends, so a shared
        /// anchor between two SmoothStepped halves stalls — measured live, the aim point all but
        /// stopped moving for about a degree around the anchor before catching back up.
        ///
        /// Angles are defensively clamped non-decreasing (steep to at least middle, top to at least
        /// steep) so a misconfigured Steep angle degrades to a shorter segment instead of an inverted
        /// or divide-by-zero one.
        /// </summary>
        public static float InterpolateByAngle(
            float orbitAngle,
            float bottomAngle, float bottom,
            float middleAngle, float middle,
            float steepAngle, float steep,
            float topAngle, float top)
        {
            steepAngle = Mathf.Max(steepAngle, middleAngle);
            topAngle = Mathf.Max(topAngle, steepAngle);

            if (orbitAngle <= bottomAngle) return bottom;
            if (orbitAngle >= topAngle) return top;

            float slopeBottomMiddle = SegmentSlope(bottomAngle, bottom, middleAngle, middle);
            float slopeMiddleSteep = SegmentSlope(middleAngle, middle, steepAngle, steep);
            float slopeSteepTop = SegmentSlope(steepAngle, steep, topAngle, top);

            float middleSlope = HarmonicSlope(slopeBottomMiddle, slopeMiddleSteep);
            float steepSlope = HarmonicSlope(slopeMiddleSteep, slopeSteepTop);

            if (orbitAngle <= middleAngle)
                return Hermite(orbitAngle, bottomAngle, middleAngle, bottom, middle, 0f, middleSlope);

            if (orbitAngle <= steepAngle)
                return Hermite(orbitAngle, middleAngle, steepAngle, middle, steep, middleSlope, steepSlope);

            return Hermite(orbitAngle, steepAngle, topAngle, steep, top, steepSlope, 0f);
        }

        private static float SegmentSlope(float x0, float y0, float x1, float y1)
        {
            float span = x1 - x0;
            return span > 0f ? (y1 - y0) / span : 0f;
        }

        // Zero only when the two segments disagree in sign — i.e. the shared point is a real local
        // min/max — which is also exactly when this formula would otherwise divide by a near-zero
        // (slopeA + slopeB), so the same check guards both cases.
        private static float HarmonicSlope(float slopeA, float slopeB) =>
            slopeA * slopeB <= 0f ? 0f : 2f * slopeA * slopeB / (slopeA + slopeB);

        // Cubic Hermite basis, tangents scaled by the segment's own span so m0/m1 (in units per
        // degree) convert correctly into the unit interval t uses.
        private static float Hermite(float x, float x0, float x1, float p0, float p1, float m0, float m1)
        {
            float span = x1 - x0;
            if (span <= 0f)
                return p0;

            float t = Mathf.Clamp01((x - x0) / span);
            float t2 = t * t, t3 = t2 * t;
            float h00 = 2f * t3 - 3f * t2 + 1f;
            float h10 = t3 - 2f * t2 + t;
            float h01 = -2f * t3 + 3f * t2;
            float h11 = t3 - t2;

            return h00 * p0 + h10 * m0 * span + h01 * p1 + h11 * m1 * span;
        }
    }
}
