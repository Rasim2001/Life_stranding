using System;
using System.Collections.Generic;
using PickupObjects;

namespace Infastructure.Services.Ability
{
    public interface IAbilityService
    {
        void PickUpAbility(ProductType product);
        bool IsExploredAbility(ProductType pickedProduct);
        List<ProductType> GetAllExploredAbilities();
        event Action<ProductType> OnAbilityAddHappened;
    }
}