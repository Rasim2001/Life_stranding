using Common.Antenna;
using Infastructure.Services.GeneratorLaunchTracker;
using R3;
using UnityEngine;
using Zenject;

namespace Common.Biosphere
{
    public class Biosphere : MonoBehaviour
    {
        [SerializeField] private BiosphereFx _biosphereFx;
        [SerializeField] private AntennaVisual[] _antennaVisuals;

        private readonly float _launchOffset = 0.25f;
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        private IGeneratorLaunchTrackerService _generatorLaunchTrackerService;

        private float _summary;
        private int _activeAntenna;

        [Inject]
        public void Construct(IGeneratorLaunchTrackerService generatorLaunchTrackerService) =>
            _generatorLaunchTrackerService = generatorLaunchTrackerService;

        private void Awake()
        {
            _generatorLaunchTrackerService.OnLaunchHappened
                .Subscribe(_ => GeneratorLaunched())
                .AddTo(_disposable);
        }

        private void OnDestroy() =>
            _disposable.Dispose();

        private void GeneratorLaunched()
        {
            _summary += _launchOffset;

            _antennaVisuals[_activeAntenna].Show();
            _activeAntenna++;

            _biosphereFx.ShowFx(_summary);
        }
    }
}