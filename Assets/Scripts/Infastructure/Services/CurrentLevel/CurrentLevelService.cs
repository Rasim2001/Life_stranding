using System;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.World;

namespace Infastructure.Services.CurrentLevel
{
    public class CurrentLevelService : ICurrentLevelService
    {
        private readonly IStaticDataService _staticDataService;

        private WorldCatalog Catalog
        {
            get
            {
                WorldCatalog catalog = _staticDataService.GameStaticData.WorldCatalog;

                if (catalog == null)
                    throw new InvalidOperationException("GameStaticData.WorldCatalog is not assigned.");

                return catalog;
            }
        }

        public CurrentLevelService(IStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;
        }

        public string LevelDataKey => Catalog.LevelDataKey;
        public bool ShowsFirstEncounter => Catalog.ShowsFirstEncounter;
    }
}
