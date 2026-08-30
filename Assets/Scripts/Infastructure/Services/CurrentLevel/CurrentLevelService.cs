using System;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.World;

namespace Infastructure.Services.CurrentLevel
{
    public class CurrentLevelService : ICurrentLevelService
    {
        private readonly IStaticDataService _staticDataService;

        private TowerCatalog Catalog
        {
            get
            {
                TowerCatalog catalog = _staticDataService.GameStaticData.TowerCatalog;

                if (catalog == null)
                    throw new InvalidOperationException("GameStaticData.TowerCatalog is not assigned.");

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
