using System;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.PlayerInput;
using UnityEngine;
using Zenject;

namespace SpiderController.SpiderMove
{
    public class RotateLegRaycast : MonoBehaviour
    {
        [SerializeField] private SpiderLegId _legType;

        private ICameraProviderService _cameraProviderService;
        private IInputService _inputService;

        private const float RotationAmount = 15f;

        [Inject]
        public void Construct(ICameraProviderService cameraProviderService, IInputService inputService)
        {
            _inputService = inputService;
            _cameraProviderService = cameraProviderService;
        }

        private void Update() =>
            RotateAccordingToInput(_inputService.InputVector);


        private void RotateAccordingToInput(Vector3 input)
        {
            if (input.sqrMagnitude < 0.01f)
                return;

            Vector3 rotationDirection = GetRotationDirection(input);

            Quaternion deltaRotation = Quaternion.AngleAxis(
                RotationAmount,
                rotationDirection
            );

            transform.localRotation *= deltaRotation;
        }

        private Vector3 GetRotationDirection(Vector3 input)
        {
            Vector3 dir = input.normalized;

            switch (_legType)
            {
                case SpiderLegId.FrontLeft:
                    return Vector3.Cross(_cameraProviderService.CameraTransform.up, dir);

                case SpiderLegId.FrontRight:
                    return Vector3.Cross(dir, _cameraProviderService.CameraTransform.up);

                case SpiderLegId.BackLeft:
                    return Vector3.Cross(dir, _cameraProviderService.CameraTransform.up);

                case SpiderLegId.BackRight:
                    return Vector3.Cross(_cameraProviderService.CameraTransform.up, dir);

                default:
                    return Vector3.up;
            }
        }
    }

    public enum SpiderLegId
    {
        FrontLeft,
        FrontRight,
        BackLeft,
        BackRight
    }
}