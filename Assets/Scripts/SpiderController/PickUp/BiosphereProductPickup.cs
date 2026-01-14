using System.Linq;
using Common;
using Common.Biosphere;
using Infastructure.Common.Pickup;
using Infastructure.Services.PlayerInput;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.TriggerChecker;
using UnityEngine;

namespace SpiderController.PickUp
{
    public class BiosphereProductPickup
    {
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly BiosphereChecker _biosphereChecker;
        private readonly Flower _flower;

        public BiosphereProductPickup(
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            BiosphereChecker biosphereChecker,
            Flower flower)
        {
            _flower = flower;
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _biosphereChecker = biosphereChecker;
        }

        public void Initialize() =>
            _biosphereChecker.OnRemoveHappened += Hide;

        public void Destroy() =>
            _biosphereChecker.OnRemoveHappened -= Hide;

        public void Update()
        {
            if (_inputService.PickupPressed)
            {
                if (_flower.IsOnPlatform)
                    Putdown();
            }

            TryShow();
        }

        private void TryShow()
        {
            foreach (Collider collider in _biosphereChecker.Results)
            {
                if (collider.TryGetComponent(out BiosphereCheckpointIndicator biosphereWin) && _flower.IsPuttingDown == false)
                    _pickupDisplayer.Show(biosphereWin.PickupDisplayPoint);
            }
        }

        private void Hide(Collider obj)
        {
            if (obj.TryGetComponent(out BiosphereCheckpointIndicator biosphereWin))
                _pickupDisplayer.Hide(biosphereWin.PickupDisplayPoint);
        }

        private void Putdown()
        {
            Collider biosphere = _biosphereChecker.Results.FirstOrDefault();

            if (biosphere == null)
                return;

            BiosphereCheckpointIndicator biosphereCheckpointIndicator = biosphere.GetComponent<BiosphereCheckpointIndicator>();
            biosphereCheckpointIndicator.StartFlowerPutdown();

            _flower.Putdown(biosphereCheckpointIndicator);
            _pickupDisplayer.Hide(biosphereCheckpointIndicator.PickupDisplayPoint);
        }
    }
}