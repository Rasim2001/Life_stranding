using System.Collections.Generic;
using Infastructure.Services.CutScene;
using PickupObjects;

namespace Infastructure.Services.Ability
{
    public class AbilityService : IAbilityService
    {
        private readonly ICutSceneService _cutSceneService;
        private readonly List<ProductType> _pickedProducts = new List<ProductType>();

        public AbilityService(ICutSceneService cutSceneService) =>
            _cutSceneService = cutSceneService;

        public void PickUpAbility(ProductType product)
        {
            if (!_pickedProducts.Contains(product))
                _pickedProducts.Add(product);
        }

        public bool IsExploredAbility(ProductType pickedProduct) =>
            _cutSceneService.IsActive || _pickedProducts.Contains(pickedProduct);
    }
}