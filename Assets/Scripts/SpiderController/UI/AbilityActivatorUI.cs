using Infastructure.Services.Ability;
using PickupObjects;
using UnityEngine;
using Zenject;

namespace SpiderController.UI
{
    public class AbilityActivatorUI : MonoBehaviour
    {
        [SerializeField] private ProductType _product;
        [SerializeField] private GameObject _container;

        private IAbilityService _abilityService;

        [Inject]
        public void Construct(IAbilityService abilityService) =>
            _abilityService = abilityService;

        private void Start()
        {
            if (!_abilityService.IsExploredAbility(_product))
                _container.SetActive(false);

            _abilityService.OnAbilityAddHappened += TryShowUI;
        }

        private void OnDestroy() =>
            _abilityService.OnAbilityAddHappened -= TryShowUI;

        private void TryShowUI(ProductType productType)
        {
            if (productType == _product)
                _container.SetActive(true);
        }
    }
}