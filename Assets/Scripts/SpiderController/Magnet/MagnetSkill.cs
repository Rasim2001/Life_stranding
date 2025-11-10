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
        private readonly StateMachineData _stateMachineData;
        private readonly EnergySystem _energySystem;
        private readonly SpiderUI _spiderUI;
        private readonly IStaticDataService _staticDataService;
        private readonly IAbilityService _abilityService;
        private readonly IMagnetFreezingService _magnetFreezingService;

        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        public MagnetSkill(
            IWindowService windowService,
            IInputService inputService,
            IStaticDataService staticDataService,
            IAbilityService abilityService,
            IMagnetFreezingService magnetFreezingService,
            StateMachineData stateMachineData,
            EnergySystem energySystem,
            SpiderUI spiderUI)
        {
            _windowService = windowService;
            _stateMachineData = stateMachineData;
            _energySystem = energySystem;
            _spiderUI = spiderUI;
            _staticDataService = staticDataService;
            _abilityService = abilityService;
            _magnetFreezingService = magnetFreezingService;
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

            if (_stateMachineData.IsMouseHolding)
                _energySystem.SpendEnergy(SpiderStaticData.EnergySpendFreezingFlowerSpeed);

            if (_stateMachineData.CurrentEnergyFillAmount <= 0)
                _magnetFreezingService.Unfreeze();
        }

        private void HideMagnet()
        {
            _spiderUI.MagnetIndicatorUI.Hide();
            _magnetFreezingService.Unfreeze();

            _stateMachineData.IsMouseHolding = false;

            _spiderUI.EnergyBar.PlayFadeHologramEffect();
        }

        private void ShowMagnet()
        {
            _spiderUI.MagnetIndicatorUI.Show();
            _magnetFreezingService.Freeze();

            _stateMachineData.IsMouseHolding = true;

            _spiderUI.EnergyBar.ShowHologram();
        }
    }
}