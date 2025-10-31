using System.Linq;
using CheckPointManagement;
using Infastructure.Common.Pickup;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using PickupObjects.PickUpOnPlatform;
using SpiderController.TriggerChecker;
using SpiderController.UI.Health;
using UnityEngine;

namespace SpiderController.PickUp
{
    public class CheckpointPickup
    {
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly IWindowService _windowService;
        private readonly CheckpointChecker _checkpointChecker;
        private readonly Flower _flower;
        private readonly HealthBarUI _healthBarUI;

        public CheckpointPickup(
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IWindowService windowService,
            CheckpointChecker checkpointChecker,
            Flower flower,
            HealthBarUI healthBarUI)
        {
            _flower = flower;
            _healthBarUI = healthBarUI;
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _windowService = windowService;
            _checkpointChecker = checkpointChecker;
        }

        public void Initialize() =>
            _checkpointChecker.OnRemoveHappened += Hide;

        public void Destroy() =>
            _checkpointChecker.OnRemoveHappened -= Hide;

        public void Update()
        {
            if (_inputService.PickupPressed)
            {
                if (_flower.IsPuttingDown)
                    PickUp();
                else if (_flower.IsOnPlatform)
                    Putdown();
            }

            TryShow();
        }

        private void TryShow()
        {
            foreach (Collider collider in _checkpointChecker.Results)
            {
                if (collider.TryGetComponent(out CheckPoint checkPoint) &&
                    (_flower.IsPuttingDown && checkPoint.IsReady ||
                     _flower.IsPuttingDown == false && checkPoint.IsReady == false))
                    _pickupDisplayer.Show(collider.transform);
            }
        }

        private void Hide(Collider obj) =>
            _pickupDisplayer.Hide(obj.transform);

        private void Putdown()
        {
            Collider checkPointCollider = _checkpointChecker.Results.FirstOrDefault();

            if (checkPointCollider == null)
                return;

            CheckPoint checkPoint = checkPointCollider.GetComponent<CheckPoint>();
            if (checkPoint.IsReady)
                return;

            checkPoint.StartFlowerPutdown();

            _flower.Putdown(checkPoint);
            _healthBarUI.PlayFadeHologramEffect();
            _pickupDisplayer.Hide(checkPointCollider.transform);
        }

        private void PickUp()
        {
            Collider checkPointCollider = _checkpointChecker.Results.FirstOrDefault();

            if (checkPointCollider == null)
                return;

            CheckPoint checkPoint = checkPointCollider.GetComponent<CheckPoint>();
            if (!checkPoint.IsReady)
                return;

            checkPoint.StartFlowerPickup();

            _flower.PickUpAfterPutdown();
            _pickupDisplayer.Hide(checkPointCollider.transform);
        }
    }
}