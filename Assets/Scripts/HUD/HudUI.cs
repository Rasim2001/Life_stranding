using System;
using DG.Tweening;
using GameDevBuddies;
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
        [SerializeField] private Transform _arrowContainer;
        public FlowerPointIndicator FlowerPointIndicator => _flowerPointIndicator;

        private FinishPointIndicator _finishPointIndicator;
        private FlowerPointIndicator _flowerPointIndicator;

        private RectTransform _arrowUIPrefab;
        private CanvasGroup _canvasGroup;
        private ICutSceneService _cutSceneService;

        private bool _cutSceneIsActive;

        [Inject]
        public void Construct(ICutSceneService cutSceneService) =>
            _cutSceneService = cutSceneService;

        private void Awake()
        {
            _canvasGroup = _arrowContainer.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
        }


        public void Initialize(RectTransform arrowUIPrefab) =>
            _arrowUIPrefab = arrowUIPrefab;

        public void RegisterFinishTarget(Transform finishTargetTransform)
        {
            RectTransform arrowUI = Instantiate(_arrowUIPrefab, _arrowContainer);

            _finishPointIndicator = new
                FinishPointIndicator(arrowUI, _canvasRectTransform, _finishPointLayer, finishTargetTransform);
        }

        public void RegisterFlowerPoint(Transform flowerTransform)
        {
            RectTransform arrowUI = Instantiate(_arrowUIPrefab, _arrowContainer);

            _flowerPointIndicator =
                new FlowerPointIndicator(arrowUI, _canvasRectTransform, _flowerPointLayer, flowerTransform);
        }


        private void Start()
        {
            _cutSceneService.OnCutsceneActiveChanged += CutsceneActiveChanged;
            TerrainScan.Instance.OnTerrainScanStart += TerrainStartHappened;

            CinemachineCore.CameraUpdatedEvent.AddListener(UpdateIndicator);
        }

        private void OnDestroy()
        {
            _cutSceneService.OnCutsceneActiveChanged -= CutsceneActiveChanged;

            if (TerrainScan.Instance != null)
                TerrainScan.Instance.OnTerrainScanStart -= TerrainStartHappened;

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

        private void TerrainStartHappened(TerrainScanInfo obj)
        {
            _canvasGroup.DOKill();
            _canvasGroup.alpha = 1;
            _canvasGroup.DOFade(0, 2).SetDelay(20);
        }
    }
}