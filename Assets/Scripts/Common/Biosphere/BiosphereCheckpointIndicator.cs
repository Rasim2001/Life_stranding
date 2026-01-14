using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Services.GeneratorLaunchTracker;
using Infastructure.Services.Window;
using UnityEngine;
using Zenject;

namespace Common.Biosphere
{
    public class BiosphereCheckpointIndicator : MonoBehaviour, ICheckpointInfo
    {
        [SerializeField] private ObserverTrigger _observerTrigger;
        [SerializeField] private Transform _glassTransform;
        [SerializeField] private Transform _pickUpDisplayPoint;
        [SerializeField] private Transform _flowerPutdownPoint;
        public Transform PickupDisplayPoint => _pickUpDisplayPoint;
        public Vector3 FlowerPutdownPosition => _flowerPutdownPoint.position;
        public Quaternion FlowerPutdownRotation => _flowerPutdownPoint.rotation;

        private IGeneratorLaunchTrackerService _generatorLaunchTrackerService;

        private float _summary;
        private Tween _rotateGlassTween;
        private bool _isTriggered;
        private IWindowService _windowService;


        [Inject]
        public void Construct(IWindowService windowService) =>
            _windowService = windowService;

        private void Start() =>
            _observerTrigger.OnTriggerEnterHappened += TriggerEnter;

        private void OnDestroy() =>
            _observerTrigger.OnTriggerEnterHappened -= TriggerEnter;

        public void StartFlowerPutdown()
        {
            RotateGlass(180);
            ShowWinWindow().Forget();
        }

        private void TriggerEnter(Collider obj)
        {
            if (_isTriggered)
                return;

            _isTriggered = true;

            RotateGlass(0);
        }


        private void RotateGlass(float angle)
        {
            _rotateGlassTween?.Kill();
            _rotateGlassTween = _glassTransform
                .DOLocalRotate(new Vector3(0, 0, angle), 1)
                .SetEase(Ease.Linear);
        }

        private async UniTask ShowWinWindow()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1.5f));

            _windowService.OpenWinPopup();
        }
    }
}