using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using SpiderController.UI.Stickers;
using UnityEngine;
using Zenject;

namespace SpiderController.UI.Health
{
    public class SpiderUI : MonoBehaviour
    {
        [SerializeField] private StickerUI _stickerUI;
        [SerializeField] private HealthBarUI _healthBarUI;
        [SerializeField] private EnergyBarUI _energyBarUI;
        [SerializeField] private PressedMouseButtonIndicatorUI _planeIndicatorUI;
        [SerializeField] private PressedMouseButtonIndicatorUI _magnetIndicatorUI;
        public SpiderHealth SpiderHealth => _spiderHealth;
        public EnergyBarUI EnergyBar => _energyBarUI;
        public PressedMouseButtonIndicatorUI PlaneIndicatorUI => _planeIndicatorUI;
        public PressedMouseButtonIndicatorUI MagnetIndicatorUI => _magnetIndicatorUI;
        public StickerUI StickerUI => _stickerUI;
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