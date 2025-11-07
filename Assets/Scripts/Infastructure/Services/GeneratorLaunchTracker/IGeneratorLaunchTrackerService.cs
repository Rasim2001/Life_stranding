using System;

namespace Infastructure.Services.GeneratorLaunchTracker
{
    public interface IGeneratorLaunchTrackerService
    {
        event Action OnGeneratorLaunchHappened;
    }
}