using DG.Tweening;
using Infastructure.Services.GeneratorLaunchTracker;
using R3;
using UnityEngine;
using Zenject;

namespace Common
{
    public class Bridge : MonoBehaviour
    {
        [SerializeField] private ObserverTrigger _observerTrigger;
        [SerializeField] private Transform _bridgeTransform;

        private readonly CompositeDisposable _disposable = new();
        private IGeneratorLaunchTrackerService _generatorLaunchTrackerService;

        private bool _isAvailable;

        [Inject]
        public void Construct(IGeneratorLaunchTrackerService generatorLaunchTrackerService) =>
            _generatorLaunchTrackerService = generatorLaunchTrackerService;

        private void Start()
        {
            _generatorLaunchTrackerService.OnLaunchHappened
                .Take(1)
                .Subscribe(_ => _isAvailable = true)
                .AddTo(_disposable);

            _observerTrigger.OnTriggerEnterHappened += TriggerEnter;
        }


        private void OnDestroy()
        {
            _observerTrigger.OnTriggerEnterHappened -= TriggerEnter;

            _disposable.Dispose();
        }

        private void TriggerEnter(Collider obj)
        {
            if (!_isAvailable)
                return;

            _bridgeTransform.DOLocalRotate(Vector3.zero, 2f);
            _isAvailable = false;
        }
    }
}