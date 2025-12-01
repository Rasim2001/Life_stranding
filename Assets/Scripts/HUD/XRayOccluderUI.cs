using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace HUD
{
    public class XRayOccluderUI : MonoBehaviour
    {
        [SerializeField] private Image _image;
        private RectTransform uiElement => _image.rectTransform;

        public Transform TargetWorldObject { get; private set; }

        private readonly Vector3 _offset = new Vector3(0, 1.4f, 0);
        private Camera _mainCamera;

        public void Initialize(Transform targetWorldObject, Sprite sprite)
        {
            TargetWorldObject = targetWorldObject;
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
            if (TargetWorldObject == null || uiElement == null)
                return;

            Vector3 screenPos = _mainCamera.WorldToScreenPoint(TargetWorldObject.position + _offset);

            if (screenPos.z > 0)
                uiElement.position = screenPos;
        }
    }
}