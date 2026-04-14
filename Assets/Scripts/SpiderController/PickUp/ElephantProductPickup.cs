using System.Linq;
using Infastructure.Common.Pickup;
using Infastructure.PlatformRegistry;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using PickupObjects.PickUpOnPlatform;
using SpiderController.Platform;
using SpiderController.TriggerChecker;
using UnityEngine;

namespace SpiderController.PickUp
{
    public class ElephantProductPickup
    {
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly IPlatformObjectsService _platformObjectsService;
        private readonly IPlatformRegistryService _platformRegistryService;
        private readonly ElephantChecker _elephantChecker;

        private bool _isShowed;

        public ElephantProductPickup(
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IPlatformObjectsService platformObjectsService,
            IPlatformRegistryService platformRegistryService,
            ElephantChecker elephantChecker)
        {
            _platformObjectsService = platformObjectsService;
            _platformRegistryService = platformRegistryService;
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
            if (CanPickup())
                PickElephant();


            TryShow();
        }

        private bool CanPickup()
        {
            return _inputService.PickupPressed &&
                   _platformObjectsService.IsEmpty() &&
                   _platformRegistryService.CurrentPlatformId == PlatformId.Surf;
        }

        private void TryShow()
        {
            foreach (Collider collider in _elephantChecker.Results)
            {
                if (collider != null && collider.TryGetComponent(out ElephantProduct elephantProduct))
                {
                    if (elephantProduct.Rigidbody.IsSleeping() && !elephantProduct.IsOnPlatform)
                        _pickupDisplayer.Show(elephantProduct.transform);
                }
            }
        }


        private void PickElephant()
        {
            Collider elephantCollider = _elephantChecker.Results.FirstOrDefault();
            if (elephantCollider == null)
                return;

            HideElephant(elephantCollider);

            ElephantProduct elephantProduct = elephantCollider.GetComponent<ElephantProduct>();
            //elephantProduct.StopSimulatePhysics();  //TODO:
        }

        private void HideElephant(Collider obj) =>
            _pickupDisplayer.Hide(obj.transform);
    }
}