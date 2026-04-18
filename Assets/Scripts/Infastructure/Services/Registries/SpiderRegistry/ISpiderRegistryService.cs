using System;
using SpiderController;

namespace Infastructure.Services.Registries.SpiderRegistry
{
    public interface ISpiderRegistryService
    {
        event Action OnSpiderInitialized;
        Spider Spider { get; }
        void RegisterSpider(Spider spider);
    }
}