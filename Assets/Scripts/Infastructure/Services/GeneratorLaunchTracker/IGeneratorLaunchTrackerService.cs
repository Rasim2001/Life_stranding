using R3;

namespace Infastructure.Services.GeneratorLaunchTracker
{
    public interface IGeneratorLaunchTrackerService
    {
        Observable<Unit> OnLaunchHappened { get; }
        void Launch();
    }
}