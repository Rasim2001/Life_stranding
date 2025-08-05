using System;
using _2;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;

namespace SpiderController
{
    public class SpiderUI : MonoBehaviour
    {
        [SerializeField] private HealthBarUI _healthBarUI;
        [SerializeField] private EnergyBarUI _energyBarUI;
        public SpiderHealth SpiderHealth => _spiderHealth;
        public EnergyBarUI EnergyBar => _energyBarUI;
        
        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private SpiderHealth _spiderHealth;
        private IStaticDataService _staticDataService;

        [Inject]
        public void Construct(IStaticDataService staticDataService) =>
            _staticDataService = staticDataService;

        public void Initialize() =>
            _spiderHealth = new SpiderHealth(SpiderStaticData.MaxHealth);

        private void Start() =>
            _spiderHealth.HealthChanged += UpdateHealthBar;

        private void OnDestroy() =>
            _spiderHealth.HealthChanged -= UpdateHealthBar;

        private void UpdateHealthBar() =>
            _healthBarUI.SetValue(_spiderHealth.CurrentHP, _spiderHealth.MaxHp);
    }
}