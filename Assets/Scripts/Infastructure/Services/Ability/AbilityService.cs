using System;
using System.Collections.Generic;
using Infastructure.Common;
using Infastructure.Data;
using Infastructure.Services.CutScene;
using Infastructure.Services.ProgressWatchers;
using Infastructure.Services.SaveLoadService;
using Infastructure.StaticData.Cheats;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;

namespace Infastructure.Services.Ability
{
    public class AbilityService : IAbilityService, ISavedProgress
    {
        public event Action<ProductType> OnAbilityAddHappened;

        private readonly ICutSceneService _cutSceneService;
        private readonly IProgressWatchersService _progressWatchersService;
        private List<ProductType> _pickedProducts = new List<ProductType>();
        private readonly IStaticDataService _staticDataService;
        private readonly ISceneLoader _sceneLoader;
        private CheatsStaticData Cheats => _staticDataService.CheatsStaticData;

        public AbilityService(ICutSceneService cutSceneService, IProgressWatchersService progressWatchersService,
            IStaticDataService staticDataService, ISceneLoader sceneLoader)
        {
            _staticDataService = staticDataService;
            _sceneLoader = sceneLoader;
            _cutSceneService = cutSceneService;
            _progressWatchersService = progressWatchersService;
        }

        public void Initialize() =>
            _progressWatchersService.RegisterWatcher(this);

        public void LoadProgress(PlayerProgress progress) =>
            _pickedProducts = new List<ProductType>(progress.AbilityData.PickedProducts);

        public void UpdateProgress(PlayerProgress progress) =>
            progress.AbilityData.PickedProducts = new List<ProductType>(_pickedProducts);

        public void PickUpAbility(ProductType product)
        {
            if (!_pickedProducts.Contains(product))
            {
                _pickedProducts.Add(product);
                OnAbilityAddHappened?.Invoke(product);
            }
        }

        public bool IsExploredAbility(ProductType pickedProduct)
        {
            return !_sceneLoader.IsTutorialScene() ||
                   Cheats.ProductsPopupEnabled ||
                   _cutSceneService.IsActive ||
                   _pickedProducts.Contains(pickedProduct);
        }

        public List<ProductType> GetAllExploredAbilities() =>
            _pickedProducts;

        public void Dispose()
        {
            _progressWatchersService.Release(this);


            _pickedProducts.Clear();
        }
    }
}