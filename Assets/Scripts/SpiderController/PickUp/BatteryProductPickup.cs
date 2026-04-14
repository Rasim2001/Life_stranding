using System.Collections.Generic;
using System.Linq;
using Infastructure.Common.Pickup;
using Infastructure.Services.Hint;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using PickupObjects.PickUpOnPlatform;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.TriggerChecker;
using UnityEngine;

namespace SpiderController.PickUp
{
    public class BatteryProductPickup
    {
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly BatteryProductChecker _batteryProductChecker;
        private readonly FlowerChecker _flowerChecker;
        private readonly IPlatformObjectsService _platformObjectsService;
        private IHintReceiverService _hintReceiverService;

        private bool _isShowed;

        public BatteryProductPickup(
            IHintReceiverService hintReceiverService,
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IPlatformObjectsService platformObjectsService,
            BatteryProductChecker batteryProductChecker,
            FlowerChecker flowerChecker)
        {
            _hintReceiverService = hintReceiverService;
            _platformObjectsService = platformObjectsService;
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _batteryProductChecker = batteryProductChecker;
            _flowerChecker = flowerChecker;
        }

        public void Initialize() =>
            _batteryProductChecker.OnRemoveHappened += HideBattery;

        public void Destroy() =>
            _batteryProductChecker.OnRemoveHappened -= HideBattery;

        public void Update()
        {
            if (_inputService.PickupPressed)
            {
                if ( /*_flowerChecker.IsTouching && */_batteryProductChecker.Results.Count > 0)
                    _hintReceiverService.OnProductHint?.Invoke();

                if (CanPickup())
                    PickBatteries();
            }

            TryShow();
        }

        private bool CanPickup()
        {
            return !_platformObjectsService.HasAny<Flower>() && !_platformObjectsService.HasAny<ElephantProduct>() &&
                /*!_flowerChecker.IsTouching &&*/ _batteryProductChecker.Results.Any(IsBatteryReadyToPickup);
        }

        private void TryShow()
        {
            foreach (Collider collider in _batteryProductChecker.Results)
            {
                if (IsBatteryReadyToPickup(collider))
                    _pickupDisplayer.Show(collider.transform);
            }
        }

        private bool IsBatteryReadyToPickup(Collider col)
        {
            if (col == null)
                return false;

            if (!col.TryGetComponent(out BatteryProduct battery))
                return false;

            return battery.Rigidbody.IsSleeping() &&
                   !battery.IsOnPlatform &&
                   !battery.IsPuttingDown;
        }

        private void PickBatteries()
        {
            List<BatteryProduct> batteryProducts = _batteryProductChecker.Results
                .Select(x => x.GetComponent<BatteryProduct>())
                .Where(x => !x.IsPuttingDown)
                .ToList();

            int n = batteryProducts.Count;

            int columns = Mathf.CeilToInt(Mathf.Sqrt(n));
            int rows = Mathf.CeilToInt((float)n / columns);

            float spacing = 0.01f;
            float totalWidth = (columns - 1) * spacing;
            float totalHeight = (rows - 1) * spacing;

            for (int i = 0; i < n; i++)
            {
                int row = i / columns;
                int col = i % columns;

                float x = -totalWidth / 2f + col * spacing;
                float z = -totalHeight / 2f + row * spacing;

                Vector3 offset = new Vector3(x, 0, z);

                BatteryProduct batteryProduct = batteryProducts[i].GetComponent<BatteryProduct>();
                batteryProduct.SetCustomOffsetPosition(offset);
                //batteryProduct.StopSimulatePhysics();  //TODO:

                _pickupDisplayer.Hide(batteryProduct.transform);
            }
        }

        private void HideBattery(Collider obj) =>
            _pickupDisplayer.Hide(obj.transform);
    }
}