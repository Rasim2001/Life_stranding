using System;

namespace Infastructure.Services.GeneratorLaunchTracker
{
    public interface IGeneratorLaunchTrackerService
    {
        Action OnGeneratorLaunchHappened { get; set; }
    }
}