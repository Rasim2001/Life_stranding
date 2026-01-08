using UnityEngine;

namespace Infastructure.Services.CameraProvider
{
    public interface ICameraProviderService
    {
        Transform CameraTransform { get; }
        Camera Camera { get; }
        void SetCamera(Camera camera);
    }
}