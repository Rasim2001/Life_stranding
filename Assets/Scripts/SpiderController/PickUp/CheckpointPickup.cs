using System.Linq;
using Common;
using Infastructure.Common.Pickup;
using Infastructure.Services.Hint;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using PickupObjects.PickUpOnPlatform;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.TriggerChecker;
using SpiderController.UI.Health;
using UI;
using UnityEngine;

namespace SpiderController.PickUp
{
    public class CheckpointPickup
    {
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly IWindowService _windowService;
        private readonly IHintService _hintService;

        private readonly CheckpointChecker _checkpointChecker;
        private readonly Flower _flower;
        private readonly SpiderUI _spiderUI;

        public CheckpointPickup(
            IHintService hintService,
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IWindowService windowService,
            CheckpointChecker checkpointChecker,
            Flower flower,
            SpiderUI spiderUI)
        {
            _hintService = hintService;
            _flower = flower;
            _spiderUI = spiderUI;
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
                if (!_flower.IsOnPlatform)
                {
                    Collider checkPointCollider = _checkpointChecker.Results.FirstOrDefault();

                    if (checkPointCollider != null)
                        _hintService.OnCheckpointHint?.Invoke();
                }


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

            checkPoint.StartFlowerPutdown(_flower);

            _flower.Putdown(checkPoint);
            _spiderUI.HealthBar.PlayFadeHologramEffect();
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

            _windowService.OpenTaskPopup(TaskId.GeneratorTask);

            _spiderUI.SpiderHealth.Reset();
            checkPoint.StartFlowerPickup();

            _flower.PickUpAfterPutdown();
            _pickupDisplayer.Hide(checkPointCollider.transform);
        }
    }
}