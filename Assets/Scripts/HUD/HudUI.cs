using System;
using Unity.Cinemachine;
using UnityEngine;

namespace HUD
{
    public class HudUI : MonoBehaviour
    {
        [SerializeField] private LayerMask _finishPointLayer;
        [SerializeField] private LayerMask _flowerPointLayer;
        [SerializeField] private RectTransform _canvasRectTransform;
        public FlowerPointIndicator FlowerPointIndicator => _flowerPointIndicator;

        private FinishPointIndicator _finishPointIndicator;
        private FlowerPointIndicator _flowerPointIndicator;

        private Transform _hudTransform;
        private RectTransform _arrowUIPrefab;


        public void Initialize(Transform hudTransform, RectTransform arrowUIPrefab)
        {
            _arrowUIPrefab = arrowUIPrefab;
            _hudTransform = hudTransform;
        }

        public void RegisterFinishTarget(Transform finishTargetTransform)
        {
            RectTransform arrowUI = Instantiate(_arrowUIPrefab, _hudTransform);

            _finishPointIndicator = new
                FinishPointIndicator(arrowUI, _canvasRectTransform, _finishPointLayer, finishTargetTransform);
        }

        public void RegisterFlowerPoint(Transform flowerTransform)
        {
            RectTransform arrowUI = Instantiate(_arrowUIPrefab, _hudTransform);

            _flowerPointIndicator =
                new FlowerPointIndicator(arrowUI, _canvasRectTransform, _flowerPointLayer, flowerTransform);
        }


        private void Start() =>
            CinemachineCore.CameraUpdatedEvent.AddListener(UpdateIndicator);

        private void OnDestroy() =>
            CinemachineCore.CameraUpdatedEvent.RemoveListener(UpdateIndicator);

        private void UpdateIndicator(CinemachineBrain arg0)
        {
            _flowerPointIndicator.Update();
            _finishPointIndicator.Update();
        }
    }
}