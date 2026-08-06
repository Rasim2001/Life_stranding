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
        /// </summary>
        float AimHeight { get; set; }

        void Initialize();
        void ShakeCamera(float distanceFalling);
        void SnapToTarget();
        void AlignToSpider();
    }
}