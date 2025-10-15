using System.Linq;
using Infastructure.Common.Pickup;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using PickupObjects.PickUpOnPlatform;
using SpiderController.TriggerChecker;
using UnityEngine;

namespace SpiderController.PickUp
{
    public class ElephantProductPickup
    {
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly IPlatformObjectsService _platformObjectsService;
        private readonly ElephantChecker _elephantChecker;

        private bool _isShowed;

        public ElephantProductPickup(
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IPlatformObjectsService platformObjectsService,
            ElephantChecker elephantChecker)
        {
            _platformObjectsService = platformObjectsService;
            _elephantChecker = elephantChecker;
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
        }

        public void Initialize() =>
            _elephantChecker.OnRemoveHappened += HideElephant;

        public void Destroy() =>
            _elephantChecker.OnRemoveHappened -= HideElephant;

        public void Update()
        {
            if (_inputService.PickupPressed && _platformObjectsService.IsEmpty())
                PickElephant();

            TryShow();
        }

        private void TryShow()
        {
            foreach (Collider collider in _elephantChecker.Results)
                _pickupDisplayer.Show(collider.transform);
        }


        private void PickElephant()
        {
            Collider elephantCollider = _elephantChecker.Results.FirstOrDefault();
            if (elephantCollider == null)
                return;

            ElephantProduct elephantProduct = elephantCollider.GetComponent<ElephantProduct>();
            elephantProduct.StopSimulatePhysics();
        }

        private void HideElephant(Collider obj) =>
            _pickupDisplayer.Hide(obj.transform);
    }
}