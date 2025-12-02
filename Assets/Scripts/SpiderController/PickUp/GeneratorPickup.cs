using System.Linq;
using Common;
using Infastructure.Common.Pickup;
using Infastructure.Services.Hint;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using PickupObjects.PickUpOnPlatform;
using SpiderController.TriggerChecker;
using UI;
using UnityEngine;

namespace SpiderController.PickUp
{
    public class GeneratorPickup
    {
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly IPlatformObjectsService _platformObjectsService;
        private readonly IWindowService _windowService;
        private IHintReceiverService _hintReceiverService;
        private readonly GeneratorChecker _generatorChecker;

        public GeneratorPickup(
            IHintReceiverService hintReceiverService,
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IPlatformObjectsService platformObjectsService,
            IWindowService windowService,
            GeneratorChecker generatorChecker)
        {
            _hintReceiverService = hintReceiverService;
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _platformObjectsService = platformObjectsService;
            _windowService = windowService;
            _generatorChecker = generatorChecker;
        }

        public void Initialize() =>
            _generatorChecker.OnRemoveHappened += Hide;

        public void Destroy() =>
            _generatorChecker.OnRemoveHappened -= Hide;

        public void Update()
        {
            if (_inputService.PickupPressed)
            {
                Collider generatorCollider = _generatorChecker.Results.FirstOrDefault();

                if (generatorCollider != null)
                {
                    if (_platformObjectsService.HasAny<BatteryProduct>())
                        StartGenerator(generatorCollider);
                    else
                        _hintReceiverService.OnGeneratorHint?.Invoke();
                }
            }

            TryShow();
        }

        private void TryShow()
        {
            foreach (Collider collider in _generatorChecker.Results)
            {
                if (collider.TryGetComponent(out Generator generator) && !generator.IsLaunched)
                    _pickupDisplayer.Show(generator.PickupDisplayPoint);
            }
        }

        private void Hide(Collider obj)
        {
            if (obj.TryGetComponent(out Generator generator))
                _pickupDisplayer.Hide(generator.PickupDisplayPoint);
        }

        private void StartGenerator(Collider generatorCollider)
        {
            BatteryProduct battery = _platformObjectsService.Get<BatteryProduct>();

            if (battery == null)
                return;

            Generator generator = generatorCollider.GetComponent<Generator>();
            if (generator.IsLaunched)
                return;

            _pickupDisplayer.Hide(generator.PickupDisplayPoint);
            _pickupDisplayer.Hide(battery.transform);

            generator.StartGenerator();
            battery.PutdownOnGenerator(generator);

            _windowService.OpenTaskPopup(TaskId.LastTask);
        }
    }
}