using Infastructure.Services.Ability;
using Infastructure.Services.Magnet;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using SpiderController.StateMachine;
using SpiderController.UI.Health;

namespace SpiderController.Magnet
{
    public class MagnetSkill
    {
        private readonly IInputService _inputService;
        private readonly IWindowService _windowService;
        private readonly EnergySystem _energySystem;
        private readonly IStaticDataService _staticDataService;
        private readonly IAbilityService _abilityService;
        private readonly IMagnetFreezingService _magnetFreezingService;
        private readonly SpiderStateContext _stateContext;
        private SpiderUI SpiderUI => _stateContext.SpiderUI;
        private StateMachineData Data => _stateContext.Data;

        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        public MagnetSkill(
            IWindowService windowService,
            IInputService inputService,
            IStaticDataService staticDataService,
            IAbilityService abilityService,
            IMagnetFreezingService magnetFreezingService,
            SpiderStateContext stateContext,
            EnergySystem energySystem)
        {
            _windowService = windowService;
            _energySystem = energySystem;
            _staticDataService = staticDataService;
            _abilityService = abilityService;
            _magnetFreezingService = magnetFreezingService;
            _stateContext = stateContext;
            _inputService = inputService;
        }

        public void Initialize() =>
            _windowService.OnWindowOpened += HideMagnet;

        public void Destroy() =>
            _windowService.OnWindowOpened -= HideMagnet;

        public void Update()
        {
            if (_inputService.RightMousePressed && _abilityService.IsExploredAbility(ProductType.MagnetSkillProduct))
                ShowMagnet();

            else if (_inputService.RightMouseUp)
                HideMagnet();

            if (Data.IsMouseHolding)
                _energySystem.SpendEnergy(SpiderStaticData.EnergySpendFreezingFlowerSpeed);

            if (Data.CurrentEnergyFillAmount <= 0)
                _magnetFreezingService.Unfreeze();
        }

        private void HideMagnet()
        {
            Data.IsMouseHolding = false;

            SpiderUI.MagnetIndicatorUI.Hide();
            _magnetFreezingService.Unfreeze();

            SpiderUI.EnergyBar.PlayFadeHologramEffect();
        }

        private void ShowMagnet()
        {
            SpiderUI.MagnetIndicatorUI.Show();
            _magnetFreezingService.Freeze();

            Data.IsMouseHolding = true;

            SpiderUI.EnergyBar.ShowHologram();
        }
    }
}