using Unity.Cinemachine;
using UnityEngine;

namespace Infastructure.Common.Pickup
{
    public class PickupView : MonoBehaviour
    {
        private Transform _targetWorldObject;
        private Camera _mainCamera;
        private readonly Vector3 _offset = new Vector3(0, 1.4f, 0);

        private void Awake() =>
            _mainCamera = Camera.main;

        public void Initialize(Transform targetWorldObject) =>
            _targetWorldObject = targetWorldObject;

        private void OnDisable() =>
            _targetWorldObject = null;

        private void Start() =>
            CinemachineCore.CameraUpdatedEvent.AddListener(UpdateCustom);

        private void OnDestroy() =>
            CinemachineCore.CameraUpdatedEvent.RemoveListener(UpdateCustom);

        private void UpdateCustom(CinemachineBrain arg0)
        {
            if (_targetWorldObject == null)
                return;

            Vector3 screenPos = _mainCamera.WorldToScreenPoint(_targetWorldObject.position + _offset);

            if (screenPos.z > 0)
                transform.position = screenPos;
        }
    }
}