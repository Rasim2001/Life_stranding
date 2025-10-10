using Infastructure.Common.Pickup;
using Infastructure.Services.PlayerInput;
using PickupObjects;
using SpiderController.TriggerChecker;
using UnityEngine;

namespace SpiderController.PickUp
{
    public class EnergyPickup
    {
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly EnergyChecker _energyChecker;

        public EnergyPickup(
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            EnergyChecker energyChecker)
        {
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _energyChecker = energyChecker;
        }

        public void Initialize() =>
            _energyChecker.OnRemoveHappened += Hide;

        public void Destroy() =>
            _energyChecker.OnRemoveHappened -= Hide;

        public void Update()
        {
            if (_inputService.PickupPressed)
                PickUp();

            TryShow();
        }

        private void TryShow()
        {
            foreach (Collider collider in _energyChecker.Results)
            {
                if (collider != null && collider.TryGetComponent(out Energy energy))
                    _pickupDisplayer.Show(energy.transform);
            }
        }

        private void Hide(Collider obj) =>
            _pickupDisplayer.Hide(obj.transform);

        private void PickUp()
        {
            Debug.Log("PickUp");
        }
    }
}