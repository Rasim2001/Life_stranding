using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infastructure.Common.Pickup;
using Infastructure.CutScenes;
using Infastructure.Services.CutScene;
using Infastructure.Services.Defeat;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerProgressService;
using Infastructure.Services.Window;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.TriggerChecker;
using SpiderController.UI.Health;
using UnityEngine;

namespace SpiderController.PickUp
{
    public class FlowerPickup
    {
        private FlowerChecker FlowerChecker => _stateContext.FlowerChecker;
        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;
        private SpiderUI SpiderUI => _stateContext.SpiderUI;
        private HealthBarUI HealthBar => SpiderUI.HealthBar;
        private SpiderHealth SpiderHealth => SpiderUI.SpiderHealth;
        private bool WasPicked => _progressService.PlayerProgress.WorldProgressData.CutsceneData.FlowerWasPicked;

        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly IPlatformObjectsService _platformObjectsService;
        private readonly IWindowService _windowService;
        private readonly IDefeatWindowService _defeatWindowService;
        private readonly ICutSceneService _cutSceneService;
        private readonly IStaticDataService _staticDataService;
        private readonly IPersistentProgressService _progressService;
        private readonly SpiderStateContext _stateContext;

        private readonly Flower _flower;
        private CancellationTokenSource _lifetimeCts;


        public FlowerPickup(
            SpiderStateContext stateContext,
            Flower flower,
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IPlatformObjectsService platformObjectsService,
            IWindowService windowService,
            IDefeatWindowService defeatWindowService,
            ICutSceneService cutSceneService,
            IStaticDataService staticDataService,
            IPersistentProgressService progressService)
        {
            _stateContext = stateContext;
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _platformObjectsService = platformObjectsService;
            _windowService = windowService;
            _defeatWindowService = defeatWindowService;
            _cutSceneService = cutSceneService;
            _staticDataService = staticDataService;
            _progressService = progressService;

            _flower = flower;
        }

        public void Initialize()
        {
            _flower.OnDroppedFromPlatform += DropFlowerHappened;
            _flower.OnGroundTriggered += GroundTriggered;

            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = new CancellationTokenSource();

            HealthBar.PlayFadeHologramEffect();
        }

        public void Destroy()
        {
            _flower.OnGroundTriggered -= GroundTriggered;
            _flower.OnDroppedFromPlatform -= DropFlowerHappened;

            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
        }

        public void Update()
        {
            bool canDisplay = CanDisplay();

            if (canDisplay && _inputService.PickupPressed && _platformObjectsService.IsEmpty() && !IsDeath())
            {
                if (!WasPicked)
                    PickupFlow().Forget();
                else
                {
                    _windowService.OpenProductDescriptionPopup(ProductType.Flower);

                    _platformObjectsService.Add(_flower);
                    HealthBar.PlayFadeHologramEffect();
                }
            }

            if (canDisplay && !IsDeath() && !_cutSceneService.IsActive)
                _pickupDisplayer.Show(_flower.transform);
            else
                _pickupDisplayer.Hide(_flower.transform);
        }

        private bool IsDeath() =>
            _defeatWindowService.IsDefeated;


        private bool CanDisplay() =>
            FlowerChecker.Results.Count > 0 && FlowerChecker.Results.Any(IsFlowerReadyToPickup);

        private bool IsFlowerReadyToPickup(Collider col)
        {
            if (col == null)
                return false;

            if (!col.TryGetComponent(out Flower flower))
                return false;

            return flower.Rigidbody.IsSleeping() &&
                   !flower.IsOnPlatform &&
                   !flower.IsPuttingDown;
        }

        private void DropFlowerHappened()
        {
            HealthBar.ShowHologram();
        }

        private void GroundTriggered() =>
            SpiderHealth.TakeDamage(SpiderStaticData.DamageAmount);

        private async UniTask PickupFlow()
        {
            _progressService.PlayerProgress.WorldProgressData.CutsceneData.FlowerWasPicked = true;

            CancellationToken token = _lifetimeCts.Token;

            await _cutSceneService.StartCutsceneAsync(CutsceneId.FlowerPickupCutscene, _lifetimeCts.Token);

            token.ThrowIfCancellationRequested();

            _windowService.OpenProductDescriptionPopup(ProductType.Flower);
            _platformObjectsService.Add(_flower);

            HealthBar.PlayFadeHologramEffect();
        }
    }
}