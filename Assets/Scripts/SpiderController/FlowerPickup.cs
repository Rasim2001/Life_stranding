using Infastructure.Common.Pickup;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using PickupObjects;
using SpiderController.UI.Health;

namespace SpiderController
{
    public class FlowerPickup
    {
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly IPlatformObjectsService _platformObjectsService;

        private readonly FlowerChecker _flowerChecker;
        private readonly Flower _flower;
        private readonly HealthBarUI _healthBar;

        public FlowerPickup(
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IPlatformObjectsService platformObjectsService,
            FlowerChecker flowerChecker,
            Flower flower,
            HealthBarUI healthBar)
        {
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _platformObjectsService = platformObjectsService;
            _flowerChecker = flowerChecker;
            _flower = flower;
            _healthBar = healthBar;
        }

        public void Initialize()
        {
            _flower.OnDroppedFromPlatform += DropFlowerHappened;

            _healthBar.PlayFadeHologramEffect();
        }

        public void Destroy() =>
            _flower.OnDroppedFromPlatform -= DropFlowerHappened;

        public void Update()
        {
            bool canDisplay = CanDisplay();

            if (canDisplay && _inputService.PickupPressed && !_platformObjectsService.HasAny<BatteryProduct>())
            {
                _flower.StopSimulatePhysics();
                _healthBar.PlayFadeHologramEffect();
            }

            if (canDisplay)
                _pickupDisplayer.Show(_flower.transform);
            else
                _pickupDisplayer.Hide(_flower.transform);
        }


        private bool CanDisplay() =>
            _flowerChecker.IsTouching && _flower.Rigidbody.IsSleeping() && !_flower.IsOnPlatform;

        private void DropFlowerHappened() =>
            _healthBar.ShowHologram();
    }
}