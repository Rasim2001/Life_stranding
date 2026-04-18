using UnityEngine;

namespace Cameras.SpiderCameras
{
    public interface ISpiderCamera
    {
        Vector3 ShoulderOffset { get; set; }
        float FieldOfView { get; set; }
        float Distance { get; set; }
        void Initialize();
        void ShakeCamera(float distanceFalling);
        void SnapToTarget();
        void AlignToSpider();
    }
}