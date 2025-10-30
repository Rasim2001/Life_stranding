using System;
using System.Collections.Generic;
using Infastructure.Services.CutScene;
using PickupObjects;
using Zenject;

namespace Infastructure.Services.Ability
{
    public class AbilityService : IAbilityService, IDisposable, IInitializable
    {
        private readonly ICutSceneService _cutSceneService;
        private readonly List<ProductType> _pickedProducts = new List<ProductType>();

        private bool _isCheating;

        public AbilityService(ICutSceneService cutSceneService) =>
            _cutSceneService = cutSceneService;

        public void Initialize() =>
            _isCheating = true;

        public void PickUpAbility(ProductType product)
        {
            if (!_pickedProducts.Contains(product))
                _pickedProducts.Add(product);
        }

        public bool IsExploredAbility(ProductType pickedProduct)
        {
            return _isCheating || _cutSceneService.IsActive || _pickedProducts.Contains(pickedProduct);
        }

        public void Dispose() =>
            _pickedProducts.Clear();
    }
}