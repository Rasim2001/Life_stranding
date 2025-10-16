using System.Collections.Generic;
using SpiderController.Platform;

namespace Infastructure.PlatformRegistry
{
    public class PlatformRegistryService : IPlatformRegistryService
    {
        public PlatformData CurrentPlatformData { get; set; }

        public PlatformId CurrentPlatformId { get; set; }

        private Dictionary<PlatformId, PlatformData> _platformDatas;

        public void Register(Dictionary<PlatformId, PlatformData> platformDatas) =>
            _platformDatas = new Dictionary<PlatformId, PlatformData>(platformDatas);

        public PlatformData TryGetPlatformData(PlatformId platformId) =>
            _platformDatas.ContainsKey(platformId) ? _platformDatas[platformId] : null;

        public Dictionary<PlatformId, PlatformData> GetAllPlatforms() =>
            _platformDatas;
    }
}