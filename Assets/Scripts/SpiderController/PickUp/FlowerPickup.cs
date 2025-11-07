using Infastructure.Common.Pickup;
using Infastructure.Services.Defeat;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Infastructure.StaticData.Spider;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.TriggerChecker;
using SpiderController.UI.Health;
using UnityEngine;

namespace SpiderController.PickUp
{
    public class FlowerPickup
    {
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly IPlatformObjectsService _platformObjectsService;
        private readonly IWindowService _windowService;
        private readonly IDefeatWindowService _defeatWindowService;

        private HealthBarUI HealthBar => _spiderUI.HealthBar;
        private SpiderHealth SpiderHealth => _spiderUI.SpiderHealth;

        private readonly FlowerChecker _flowerChecker;
        private readonly Flower _flower;
        private readonly SpiderUI _spiderUI;

        private readonly SpiderStaticData _spiderStaticData;

        public FlowerPickup(
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IPlatformObjectsService platformObjectsService,
            IWindowService windowService,
            IDefeatWindowService defeatWindowService,
            FlowerChecker flowerChecker,
            Flower flower,
            SpiderUI spiderUI,
            SpiderStaticData spiderStaticData)
        {
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _platformObjectsService = platformObjectsService;
            _windowService = windowService;
            _defeatWindowService = defeatWindowService;
            _flowerChecker = flowerChecker;
            _flower = flower;
            _spiderUI = spiderUI;
            _spiderStaticData = spiderStaticData;
        }

        public void Initialize()
        {
            _flower.OnDroppedFromPlatform += DropFlowerHappened;
            _flower.OnGroundTriggered += GroundTriggered;

            HealthBar.PlayFadeHologramEffect();
        }

        public void Destroy()
        {
            _flower.OnGroundTriggered -= GroundTriggered;
            _flower.OnDroppedFromPlatform -= DropFlowerHappened;
        }

        public void Update()
        {
            bool canDisplay = CanDisplay();

            if (canDisplay && _inputService.PickupPressed && _platformObjectsService.IsEmpty() && !IsDeath())
            {
                _windowService.OpenProductDescriptionPopup(ProductType.Flower);

                _flower.StopSimulatePhysics();
                HealthBar.PlayFadeHologramEffect();
            }

            if (canDisplay && !IsDeath())
                _pickupDisplayer.Show(_flower.transform);
            else
                _pickupDisplayer.Hide(_flower.transform);
        }

        private bool IsDeath() =>
            _defeatWindowService.IsDefeated;


        private bool CanDisplay() =>
            _flowerChecker.IsTouching && _flower.Rigidbody.IsSleeping() && !_flower.IsOnPlatform;

        private void DropFlowerHappened() =>
            HealthBar.ShowHologram();

        private void GroundTriggered() =>
            SpiderHealth.TakeDamage(_spiderStaticData.DamageAmount);
    }
}