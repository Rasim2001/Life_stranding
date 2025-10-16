using System.Collections.Generic;
using SpiderController.Platform;

namespace Infastructure.PlatformRegistry
{
    public interface IPlatformRegistryService
    {
        PlatformData CurrentPlatformData { get; set; }
        PlatformId CurrentPlatformId { get; set; }
        void Register(Dictionary<PlatformId, PlatformData> platformDatas);
        PlatformData TryGetPlatformData(PlatformId platformId);
        Dictionary<PlatformId, PlatformData> GetAllPlatforms();
    }
}