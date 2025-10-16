using Infastructure.Services.CutScene;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using SpiderController.UI.Stickers;
using UnityEngine;
using Zenject;

namespace SpiderController.UI.Health
{
    public class SpiderUI : MonoBehaviour
    {
        [SerializeField] private GameObject _canvasRootUI;

        [SerializeField] private HealthBarUI _healthBarUI;
        [SerializeField] private EnergyBarUI _energyBarUI;
        [SerializeField] private ReloadUI _reloadUI;
        [SerializeField] private PressedMouseButtonIndicatorUI _planeIndicatorUI;
        [SerializeField] private PressedMouseButtonIndicatorUI _magnetIndicatorUI;
        public SpiderHealth SpiderHealth => _spiderHealth;
        public HealthBarUI HealthBar => _healthBarUI;
        public EnergyBarUI EnergyBar => _energyBarUI;
        public ReloadUI ReloadUI => _reloadUI;
        public PressedMouseButtonIndicatorUI PlaneIndicatorUI => _planeIndicatorUI;
        public PressedMouseButtonIndicatorUI MagnetIndicatorUI => _magnetIndicatorUI;
        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private SpiderHealth _spiderHealth;
        private IStaticDataService _staticDataService;
        private ICutSceneService _cutSceneService;

        [Inject]
        public void Construct(IStaticDataService staticDataService, ICutSceneService cutSceneService)
        {
            _cutSceneService = cutSceneService;
            _staticDataService = staticDataService;
        }

        public void Initialize() =>
            _spiderHealth = new SpiderHealth(SpiderStaticData.MaxHealth);

        private void Start()
        {
            _cutSceneService.OnCutsceneActiveChanged += CutsceneActiveChanged;
            _spiderHealth.HealthChanged += UpdateHealthBar;
        }

        private void OnDestroy()
        {
            _cutSceneService.OnCutsceneActiveChanged -= CutsceneActiveChanged;
            _spiderHealth.HealthChanged -= UpdateHealthBar;
        }

        private void UpdateHealthBar() =>
            _healthBarUI.SetValue(_spiderHealth.CurrentHP / _spiderHealth.MaxHp);

        private void CutsceneActiveChanged(bool cutSceneIsActive) =>
            _canvasRootUI.SetActive(!cutSceneIsActive);
    }
}