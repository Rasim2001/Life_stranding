using Infastructure.Common.Pickup;
using Infastructure.Services.PlayerInput;
using PickupObjects;
using UnityEngine;

namespace SpiderController
{
    public class FlowerPickup
    {
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;

        private readonly FlowerChecker _flowerChecker;
        private readonly Flower _flower;

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
                _flower.StopSimulatePhysics();

            if (canDisplay)
                _pickupDisplayer.Show(_flower.transform);
            else
                _pickupDisplayer.Hide(_flower.transform);
        }


        private bool CanDisplay() =>
            _flowerChecker.IsTouching && _flower.Rigidbody.IsSleeping() && !_flower.IsOnPlatform;
    }
}