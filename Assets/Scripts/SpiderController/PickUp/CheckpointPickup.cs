using System.Linq;
using Infastructure.Common.Pickup;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using PickupObjects.PickUpOnPlatform;
using SpiderController.Platform;
using SpiderController.TriggerChecker;
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
        private PlatformSelector _platformSelector;

        public CheckpointPickup(
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IWindowService windowService,
            CheckpointChecker checkpointChecker,
            Flower flower,
            PlatformSelector platformSelector)
        {
            _platformSelector = platformSelector;
            _flower = flower;
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
                if (IsPutingDown())
                    PickUp();
                else
                    Putdown();
            }

            TryShow();
        }

        private void TryShow()
        {
            foreach (Collider collider in _checkpointChecker.Results)
                _pickupDisplayer.Show(collider.transform);
        }

        private void Hide(Collider obj) =>
            _pickupDisplayer.Hide(obj.transform);

        private void Putdown()
        {
            Collider skillCollider = _checkpointChecker.Results.FirstOrDefault();

            if (skillCollider == null)
                return;

            BoxCollider boxCollider = _flower.GetComponent<BoxCollider>();
            boxCollider.enabled = false;

            _flower.Rigidbody.isKinematic = true;
            _flower.IsPuttingDown = true;

            _flower.StartSimulatePhysics();

            CheckPoint checkPoint = skillCollider.GetComponent<CheckPoint>();

            _flower.transform.position = checkPoint.FlowerPutdownPosition;
            _flower.transform.rotation = checkPoint.FlowerPutdownRotation;

            _platformSelector.IsOnPlatform(boxCollider);

            _pickupDisplayer.Hide(skillCollider.transform);
        }

        private void PickUp()
        {
            Collider skillCollider = _checkpointChecker.Results.FirstOrDefault();

            if (skillCollider == null)
                return;

            _flower.GetComponent<BoxCollider>().enabled = true;

            _flower.IsPuttingDown = false;
            _flower.Rigidbody.isKinematic = false;

            _flower.StopSimulatePhysics();

            _pickupDisplayer.Hide(skillCollider.transform);
        }

        private bool IsPutingDown() =>
            _flower.GetComponent<BoxCollider>() == false;
    }
}