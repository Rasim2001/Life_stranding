using R3;
using UnityEngine;

namespace Infastructure.Services.GeneratorLaunchTracker
{
    public class GeneratorLaunchTrackerService : IGeneratorLaunchTrackerService
    {
        public Observable<Unit> OnLaunchHappened => _onLaunchHappened;

        private readonly Subject<Unit> _onLaunchHappened = new();

        public void Launch() =>
            _onLaunchHappened.OnNext(Unit.Default);
    }
}