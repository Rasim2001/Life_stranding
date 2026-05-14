using DG.Tweening;
using Infastructure.Data;
using Infastructure.Services.GeneratorLaunchTracker;
using Infastructure.Services.ProgressWatchers;
using Infastructure.Services.SaveLoadService;
using R3;
using UI;
using UnityEngine;
using Zenject;

namespace Common
{
    public class Bridge : MonoBehaviour, ISavedProgressReader
    {
        [SerializeField] private ObserverTrigger _observerTrigger;
        [SerializeField] private Transform _bridgeTransform;

        private readonly CompositeDisposable _disposable = new();
        private IGeneratorLaunchTrackerService _generatorLaunchTrackerService;

        private bool _isAvailable;
        private IProgressWatchersService _progressWatchersService;

        [Inject]
        public void Construct(IGeneratorLaunchTrackerService generatorLaunchTrackerService,
            IProgressWatchersService progressWatchersService)
        {
            _progressWatchersService = progressWatchersService;
            _generatorLaunchTrackerService = generatorLaunchTrackerService;
        }

        public void LoadProgress(PlayerProgress progress)
        {
            if (progress.TaskPopupData.CompletedTaskIds.Contains(TaskId.LastTask))
                _bridgeTransform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        private void Awake()
        {
            _progressWatchersService.RegisterWatchers(gameObject);

            _generatorLaunchTrackerService.OnLaunchHappened
                .Take(1)
                .Subscribe(_ => _isAvailable = true)
                .AddTo(_disposable);
        }

        private void Start() =>
            _observerTrigger.OnTriggerEnterHappened += TriggerEnter;


        private void OnDestroy()
        {
            _progressWatchersService.Release(this);
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