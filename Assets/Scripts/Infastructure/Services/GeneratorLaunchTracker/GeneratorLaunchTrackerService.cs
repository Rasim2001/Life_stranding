using System;

namespace Infastructure.Services.GeneratorLaunchTracker
{
    public class GeneratorLaunchTrackerService : IGeneratorLaunchTrackerService
    {
        public Action OnGeneratorLaunchHappened { get; set; }
    }
}