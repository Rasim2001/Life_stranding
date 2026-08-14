using UnityEngine;

namespace Cameras.SpiderCameras
{
    public interface ISpiderCamera
    {
        Vector3 ShoulderOffset { get; set; }
        float FieldOfView { get; set; }
        float Distance { get; set; }

        /// <summary>
        /// Additive vertical framing offset on top of the composer's authored screen position —
        /// 0 keeps whatever the artist set in the prefab, positive pushes the framed point down
        /// so the spider stays visible while the camera pitches down.
        /// </summary>
        float FramingVerticalOffset { get; set; }

        /// <summary>
        /// Height above the spider that the camera aims at, along the spider's own up (so it
        /// stays "above the spider" on slopes and walls, not offset along world Y). Lets the
        /// composition put the cargo in frame instead of centring on the spider's own origin.
        ///
        /// Loses almost all framing power as the camera swings overhead: measured, 0.85 shifts the
        /// spider 0.168 of the frame at the lower orbit limit but only 0.023 at 84° — up has turned
        /// into the view axis by then. <see cref="AimForward"/> covers that end of the range.
        /// </summary>
        float AimHeight { get; set; }

        /// <summary>
        /// How far ahead of the spider the camera aims, along the heading the camera is yawed to.
        /// The complement of <see cref="AimHeight"/>: worth 0.163 of the frame at 84° and exactly
        /// nothing at the lower orbit limit, where forward lies along the view axis instead.
        /// </summary>
        float AimForward { get; set; }

        void Initialize();
        void ShakeCamera(float distanceFalling);
        void SnapToTarget();
        void AlignToSpider();
    }
}