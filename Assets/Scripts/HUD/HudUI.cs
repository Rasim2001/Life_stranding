using System;
using Infastructure.Services.CutScene;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

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
        private ICutSceneService _cutSceneService;

        private bool _cutSceneIsActive;

        [Inject]
        public void Construct(ICutSceneService cutSceneService) =>
            _cutSceneService = cutSceneService;


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


        private void Start()
        {
            _cutSceneService.OnCutsceneActiveChanged += CutsceneActiveChanged;
            CinemachineCore.CameraUpdatedEvent.AddListener(UpdateIndicator);
        }

        private void OnDestroy()
        {
            _cutSceneService.OnCutsceneActiveChanged -= CutsceneActiveChanged;
            CinemachineCore.CameraUpdatedEvent.RemoveListener(UpdateIndicator);
        }

        private void UpdateIndicator(CinemachineBrain arg0)
        {
            if (_cutSceneIsActive)
                return;

            _flowerPointIndicator.Update();
            _finishPointIndicator.Update();
        }

        private void CutsceneActiveChanged(bool isActive)
        {
            _cutSceneIsActive = isActive;

            if (isActive)
                _finishPointIndicator.HideTargetPoint();
            else
                _finishPointIndicator.ShowTargetPoint();
        }
    }
}