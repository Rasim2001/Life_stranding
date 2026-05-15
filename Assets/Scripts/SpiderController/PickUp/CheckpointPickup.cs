using System.Linq;
using Common;
using Infastructure.Common.Pickup;
using Infastructure.Services.Hint;
using Infastructure.Services.PlatformObjects;
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
        private CheckpointChecker СheckpointChecker => _stateContext.CheckpointChecker;
        private FlowerChecker FlowerChecker => _stateContext.FlowerChecker;
        private SpiderUI SpiderUI => _stateContext.SpiderUI;

        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly IWindowService _windowService;
        private readonly IPlatformObjectsService _platformObjectsService;
        private readonly IHintReceiverService _hintReceiverService;

        private readonly SpiderStateContext _stateContext;
        private readonly Flower _flower;

        public CheckpointPickup(
            IHintReceiverService hintReceiverService,
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IWindowService windowService,
            IPlatformObjectsService platformObjectsService,
            SpiderStateContext stateContext,
            Flower flower)
        {
            _hintReceiverService = hintReceiverService;
            _flower = flower;
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _windowService = windowService;
            _platformObjectsService = platformObjectsService;
            _stateContext = stateContext;
        }

        public void Initialize() =>
            СheckpointChecker.OnRemoveHappened += Hide;

        public void Destroy() =>
            СheckpointChecker.OnRemoveHappened -= Hide;

        public void Update()
        {
            if (_inputService.PickupPressed)
            {
                if (!_flower.IsOnPlatform && (_flower.IsPuttingDown == false || !_platformObjectsService.IsEmpty()))
                {
                    Collider checkPointCollider = СheckpointChecker.Results.FirstOrDefault();

                    if (checkPointCollider != null)
                        _hintReceiverService.OnCheckpointHint?.Invoke();
                }

                if (_flower.IsPuttingDown && _platformObjectsService.IsEmpty())
                    PickUp();
                else if (_flower.IsOnPlatform)
                    Putdown();
            }

            TryShow();
        }

        private void TryShow()
        {
            foreach (Collider collider in СheckpointChecker.Results)
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
            Collider checkPointCollider = СheckpointChecker.Results.FirstOrDefault();

            if (checkPointCollider == null)
                return;

            CheckPoint checkPoint = checkPointCollider.GetComponent<CheckPoint>();
            if (checkPoint.IsReady)
                return;

            FlowerChecker.ForceRemove(_flower.Collider);

            checkPoint.StartFlowerPutdown(_flower);

            _windowService.OpenTaskPopup(TaskId.GeneratorTask);

            _flower.Putdown(checkPoint);
            _platformObjectsService.Remove(_flower);

            SpiderUI.HealthBar.PlayFadeHologramEffect();
            _pickupDisplayer.Hide(checkPointCollider.transform);
        }

        private void PickUp()
        {
            Collider checkPointCollider = СheckpointChecker.Results.FirstOrDefault();

            if (checkPointCollider == null)
                return;

            CheckPoint checkPoint = checkPointCollider.GetComponent<CheckPoint>();
            if (!checkPoint.IsReady)
                return;

            SpiderUI.SpiderHealth.Reset();
            checkPoint.StartFlowerPickup();

            _flower.PickUpAfterPutdown();
            _platformObjectsService.Add(_flower);

            _pickupDisplayer.Hide(checkPointCollider.transform);
        }
    }
}