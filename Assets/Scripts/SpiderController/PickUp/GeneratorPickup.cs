using System;
using System.Linq;
using System.Threading;
using Common;
using Cysharp.Threading.Tasks;
using Infastructure.Common.Pickup;
using Infastructure.CutScenes;
using Infastructure.Services.CutScene;
using Infastructure.Services.Hint;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerProgressService;
using Infastructure.Services.Window;
using PickupObjects.PickUpOnPlatform;
using SpiderController.TriggerChecker;
using UI;
using UnityEngine;

namespace SpiderController.PickUp
{
    public class GeneratorPickup
    {
        private GeneratorChecker GeneratorChecker => _stateContext.GeneratorChecker;
        private BatteryProductChecker BatteryProductChecker => _stateContext.BatteryChecker;

        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly IPlatformObjectsService _platformObjectsService;
        private readonly IWindowService _windowService;
        private readonly ICutSceneService _cutSceneService;
        private readonly IPersistentProgressService _progressService;
        private readonly IHintReceiverService _hintReceiverService;
        private readonly SpiderStateContext _stateContext;

        private CancellationTokenSource _lifetimeCts;
        private bool WasPicked => _progressService.PlayerProgress.WorldProgressData.CutsceneData.GeneratorWasPicked;

        public GeneratorPickup(
            IHintReceiverService hintReceiverService,
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IPlatformObjectsService platformObjectsService,
            IWindowService windowService,
            ICutSceneService cutSceneService,
            IPersistentProgressService progressService,
            SpiderStateContext stateContext)
        {
            _hintReceiverService = hintReceiverService;
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _platformObjectsService = platformObjectsService;
            _windowService = windowService;
            _cutSceneService = cutSceneService;
            _progressService = progressService;
            _stateContext = stateContext;
        }

        public void Initialize()
        {
            GeneratorChecker.OnRemoveHappened += Hide;

            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = new CancellationTokenSource();
        }

        public void Destroy()
        {
            GeneratorChecker.OnRemoveHappened -= Hide;

            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
        }

        public void Update()
        {
            if (_inputService.PickupPressed)
            {
                Collider generatorCollider = GeneratorChecker.Results.FirstOrDefault();

                if (generatorCollider != null)
                {
                    if (!WasPicked && _platformObjectsService.HasAny<BatteryProduct>())
                        StartGeneratorAsync(generatorCollider).Forget();
                    else if (_platformObjectsService.HasAny<BatteryProduct>())
                        StartGenerator(generatorCollider);
                    else
                        _hintReceiverService.OnGeneratorHint?.Invoke();
                }
            }

            TryShow();
        }

        private async UniTask StartGeneratorAsync(Collider generatorCollider)
        {
            _progressService.PlayerProgress.WorldProgressData.CutsceneData.GeneratorWasPicked = true;

            CancellationToken token = _lifetimeCts.Token;

            BatteryProduct battery = _platformObjectsService.Get<BatteryProduct>();

            if (battery == null)
                return;

            Generator generator = generatorCollider.GetComponent<Generator>();
            if (generator.IsLaunched)
                return;

            BatteryProductChecker.ForceRemove(battery.Collider);

            _pickupDisplayer.Hide(generator.PickupDisplayPoint);
            _pickupDisplayer.Hide(battery.transform);

            generator.StartGeneratorAsync().Forget();
            battery.PutdownOnGenerator(generator);

            _platformObjectsService.Remove(battery);

            await _cutSceneService.StartCutsceneAsync(CutsceneId.GeneratorCutScene, token);

            token.ThrowIfCancellationRequested();

            _windowService.OpenTaskPopup(TaskId.LastTask);
        }


        private void TryShow()
        {
            foreach (Collider collider in GeneratorChecker.Results)
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

            BatteryProductChecker.ForceRemove(battery.Collider);

            _pickupDisplayer.Hide(generator.PickupDisplayPoint);
            _pickupDisplayer.Hide(battery.transform);

            generator.StartGenerator();
            battery.PutdownOnGenerator(generator);

            _platformObjectsService.Remove(battery);

            _windowService.OpenTaskPopup(TaskId.LastTask);
        }
    }
}