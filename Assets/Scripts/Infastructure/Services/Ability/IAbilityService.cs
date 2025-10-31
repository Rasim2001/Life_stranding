using PickupObjects;

namespace Infastructure.Services.Ability
{
    public interface IAbilityService
    {
        void PickUpAbility(ProductType product);
        bool IsExploredAbility(ProductType pickedProduct);
    }
}