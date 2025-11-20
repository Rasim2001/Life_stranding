using Infastructure.Services.PlayerInput;
using UnityEngine;
using Zenject;

namespace SpiderController.SpiderMove
{
    public class RotateLegRaycast : MonoBehaviour
    {
        private const float DeadZone = 0.05f;
        private const float RotationAmount = 15f;

        private IInputService _inputService;

        private Quaternion _defaultLocalRotation;

        [Inject]
        public void Construct(IInputService inputService) =>
            _inputService = inputService;

        private void Awake() =>
            _defaultLocalRotation = transform.localRotation;

        private void Update() =>
            RotateAccordingToInput();

        private void RotateAccordingToInput()
        {
            Vector3 input = _inputService.InputVector;

            float targetAngleX = 0f;
            float targetAngleZ = 0f;

            if (Mathf.Abs(input.z) > DeadZone)
                targetAngleX += Mathf.Sign(input.z) * -RotationAmount;

            if (Mathf.Abs(input.x) > DeadZone)
                targetAngleZ += Mathf.Sign(input.x) * RotationAmount;

            Quaternion targetLocalRotation =
                _defaultLocalRotation * Quaternion.Euler(targetAngleX, 0f, targetAngleZ);

            transform.localRotation = targetLocalRotation;
        }
    }
}