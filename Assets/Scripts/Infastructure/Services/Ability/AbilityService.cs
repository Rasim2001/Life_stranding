using System;
using System.Collections.Generic;
using Infastructure.Services.CutScene;
using PickupObjects;
using Zenject;

namespace Infastructure.Services.Ability
{
    public class AbilityService : IAbilityService, IDisposable, IInitializable
    {
        public event Action<ProductType> OnAbilityAddHappened;

        private readonly ICutSceneService _cutSceneService;
        private readonly List<ProductType> _pickedProducts = new List<ProductType>();

        private bool _isCheating;

        public AbilityService(ICutSceneService cutSceneService) =>
            _cutSceneService = cutSceneService;

        public void Initialize()
        {
            _isCheating = true;
        }

        public void PickUpAbility(ProductType product)
        {
            if (!_pickedProducts.Contains(product))
            {
                _pickedProducts.Add(product);
                OnAbilityAddHappened?.Invoke(product);
            }
        }

        public bool IsExploredAbility(ProductType pickedProduct) =>
            _isCheating || _cutSceneService.IsActive || _pickedProducts.Contains(pickedProduct);

        public List<ProductType> GetAllExploredAbilities() =>
            _pickedProducts;

        public void Dispose() =>
            _pickedProducts.Clear();
    }
}