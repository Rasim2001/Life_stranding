using UnityEngine;

namespace Cameras.SpiderCameras
{
    public interface ISpiderCamera
    {
        Vector3 ShoulderOffset { get; set; }
        float FieldOfView { get; set; }
        void Initialize();
        void ShakeCamera(float distanceFalling);
    }
}