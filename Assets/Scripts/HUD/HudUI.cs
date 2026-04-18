using DG.Tweening;
using GameDevBuddies;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CutScene;
using Infastructure.Services.Registries.SpiderRegistry;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace HUD
{
    public class HudUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _canvasRectTransform;
        [SerializeField] private Transform _arrowContainer;
        [SerializeField] private Transform _xRayCollectionContainer;
        [SerializeField] private Transform _disabledContainer;
        [SerializeField] private CanvasGroup _canvasGroup;
        public FlowerPointIndicator FlowerPointIndicator => _flowerPointIndicator;
        public Transform XRayCollectionContainer => _xRayCollectionContainer;
        public Transform DisabledContainer => _disabledContainer;

        private FinishPointIndicator _finishPointIndicator;
        private FlowerPointIndicator _flowerPointIndicator;

        private ArrowUI _arrowUIPrefab;
        private TerrainScan _terrain;

        private ICutSceneService _cutSceneService;
        private ICameraProviderService _cameraProviderService;
        private ISpiderRegistryService _spiderRegistryService;

        private bool _cutSceneIsActive;


        [Inject]
        public void Construct(ICutSceneService cutSceneService, ICameraProviderService cameraProviderService,
            ISpiderRegistryService spiderRegistryService)
        {
            _spiderRegistryService = spiderRegistryService;
            _cameraProviderService = cameraProviderService;
            _cutSceneService = cutSceneService;
        }

        private void Awake() =>
            _canvasGroup.alpha = 0;


        public void Initialize(ArrowUI arrowUIPrefab) =>
            _arrowUIPrefab = arrowUIPrefab;

        public void RegisterFinishTarget(Transform finishTargetTransform)
        {
            ArrowUI arrowUI = Instantiate(_arrowUIPrefab, _arrowContainer);
            arrowUI.Initialize(_spiderRegistryService.Spider.transform, finishTargetTransform);

            _finishPointIndicator = new
                FinishPointIndicator(arrowUI, _canvasRectTransform, finishTargetTransform,
                    _cameraProviderService);
        }

        public void RegisterFlowerPoint(Flower flower)
        {
            ArrowUI arrowUI = Instantiate(_arrowUIPrefab, _arrowContainer);
            arrowUI.Initialize(_spiderRegistryService.Spider.transform, flower.transform);

            _flowerPointIndicator =
                new FlowerPointIndicator(arrowUI, _canvasRectTransform, flower,
                    _cameraProviderService);
        }


        private void Start()
        {
            _terrain = TerrainScan.Instance;
            _terrain.OnTerrainScanStart += TerrainStartHappened;

            _cutSceneService.OnCutsceneActiveChanged += CutsceneActiveChanged;

            CinemachineCore.CameraUpdatedEvent.AddListener(UpdateIndicator);
        }

        private void OnDestroy()
        {
            _cutSceneService.OnCutsceneActiveChanged -= CutsceneActiveChanged;

            if (_terrain != null)
                _terrain.OnTerrainScanStart -= TerrainStartHappened;

            _canvasGroup.DOKill();

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