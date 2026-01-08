using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Services.GeneratorLaunchTracker;
using Infastructure.Services.Window;
using R3;
using UnityEngine;
using Zenject;

namespace Common
{
    public class BiosphereWin : MonoBehaviour, ICheckpointInfo
    {
        [SerializeField] private BiosphereFx _biosphereFx;
        [SerializeField] private ObserverTrigger _observerTrigger;
        [SerializeField] private Transform _glassTransform;
        [SerializeField] private Transform _pickUpDisplayPoint;
        [SerializeField] private Transform _flowerPutdownPoint;
        public Transform PickupDisplayPoint => _pickUpDisplayPoint;
        public Vector3 FlowerPutdownPosition => _flowerPutdownPoint.position;
        public Quaternion FlowerPutdownRotation => _flowerPutdownPoint.rotation;

        private readonly float _launchOffset = 0.25f;
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        private IGeneratorLaunchTrackerService _generatorLaunchTrackerService;

        private float _summary;
        private Tween _rotateGlassTween;
        private bool _isTriggered;
        private IWindowService _windowService;


        [Inject]
        public void Construct(IGeneratorLaunchTrackerService generatorLaunchTrackerService,
            IWindowService windowService)
        {
            _windowService = windowService;
            _generatorLaunchTrackerService = generatorLaunchTrackerService;
        }

        private void Awake()
        {
            _generatorLaunchTrackerService.OnLaunchHappened
                .Subscribe(_ => GeneratorLaunched())
                .AddTo(_disposable);
        }

        private void Start() =>
            _observerTrigger.OnTriggerEnterHappened += TriggerEnter;

        private void OnDestroy()
        {
            _disposable.Dispose();

            _observerTrigger.OnTriggerEnterHappened -= TriggerEnter;
        }

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

        private void GeneratorLaunched()
        {
            _summary += _launchOffset;

            _biosphereFx.ShowFx(_summary);
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