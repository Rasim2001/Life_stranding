using Infastructure.Services.CameraProvider;
using UnityEngine;

namespace Common
{
    public class RotateToCamera
    {
        private readonly ICameraProviderService _cameraProviderService;

        public RotateToCamera(ICameraProviderService cameraProviderService) => 
            _cameraProviderService = cameraProviderService;

        public void UpdateRotation(Transform target) =>
            target.rotation = _cameraProviderService.CameraTransform.rotation;
    }
}