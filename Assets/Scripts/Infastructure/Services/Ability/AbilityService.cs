using System.Collections.Generic;
using PickupObjects;

namespace Infastructure.Services.Ability
{
    public class AbilityService : IAbilityService
    {
        private readonly List<ProductType> _pickedProducts = new List<ProductType>();

        public void PickUpAbility(ProductType product)
        {
            if (!_pickedProducts.Contains(product))
                _pickedProducts.Add(product);
        }

        public bool IsExploredAbility(ProductType pickedProduct) =>
            _pickedProducts.Contains(pickedProduct);
    }
}