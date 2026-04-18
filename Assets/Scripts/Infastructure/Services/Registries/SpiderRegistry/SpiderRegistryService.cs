using System;
using SpiderController;

namespace Infastructure.Services.Registries.SpiderRegistry
{
    public class SpiderRegistryService : ISpiderRegistryService
    {
        public event Action OnSpiderInitialized;
        public Spider Spider { get; private set; }

        public void RegisterSpider(Spider spider)
        {
            Spider = spider;
            OnSpiderInitialized?.Invoke();
        }
    }
}