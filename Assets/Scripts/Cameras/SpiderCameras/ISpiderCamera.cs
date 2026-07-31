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

        void Initialize();
        void ShakeCamera(float distanceFalling);
        void SnapToTarget();
        void AlignToSpider();
    }
}