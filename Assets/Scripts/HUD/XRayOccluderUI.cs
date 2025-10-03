using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace HUD
{
    public class XRayOccluderUI : MonoBehaviour
    {
        [SerializeField] private Image _image;
        private RectTransform uiElement => _image.rectTransform;

        private Transform _targetWorldObject;
        private Camera _mainCamera;

        public void Initialize(Transform targetWorldObject, Sprite sprite)
        {
            _targetWorldObject = targetWorldObject;
            _image.sprite = sprite;
        }

        private void Awake() =>
            _mainCamera = Camera.main;

        private void Start() =>
            CinemachineCore.CameraUpdatedEvent.AddListener(UpdateCustom);

        private void OnDestroy() =>
            CinemachineCore.CameraUpdatedEvent.RemoveListener(UpdateCustom);

        private void UpdateCustom(CinemachineBrain arg0)
        {
            if (_targetWorldObject == null || uiElement == null)
                return;

            Vector3 screenPos = _mainCamera.WorldToScreenPoint(_targetWorldObject.position);

            if (screenPos.z > 0)
                uiElement.position = screenPos;
        }
    }
}