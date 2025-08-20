using Infastructure.Common;
using Infastructure.Services.PlayerInput;
using UnityEngine;

namespace SpiderController
{
    public class FlowerPickup
    {
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;

        private readonly FlowerChecker _flowerChecker;
        private readonly Flower _flower;

        private bool _isShowed;

        public FlowerPickup(
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            FlowerChecker flowerChecker,
            Flower flower)
        {
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _flowerChecker = flowerChecker;
            _flower = flower;
        }

        public void Update()
        {
            bool canDisplay = CanDisplay();

            if (canDisplay && _inputService.PickupPressed)
                _flower.ResetSimulate();

            if (canDisplay)
                Show();
            else
                Hide();
        }

        private void Hide()
        {
            if (!_isShowed)
                return;

            _pickupDisplayer.Hide();

            _isShowed = false;
        }

        private void Show()
        {
            if (_isShowed)
                return;

            _pickupDisplayer.Show(_flower.transform);

            _isShowed = true;
        }

        private bool CanDisplay() =>
            _flowerChecker.IsTouching && _flower.Rigidbody.IsSleeping() && !_flower.IsOnPlatform;
    }
}