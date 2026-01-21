using UnityEngine;

namespace Infastructure.Services.CameraProvider
{
    public class CameraProviderService : ICameraProviderService
    {
        public Transform CameraTransform { get; private set; }
        public Camera Camera { get; private set; }

        public void SetCamera(Camera camera)
        {
            Camera = camera;
            CameraTransform = camera.transform;
        }
        
    }
}