using System.Collections.Generic;
using Common;
using Infastructure.Common.Pickup;
using Infastructure.Data;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerProgressService;
using Infastructure.Services.SaveLoadService;
using Infastructure.Services.Window;
using Infastructure.Services.XRay;
using Infastructure.StaticData.XRay;
using PickupObjects;
using SpiderController.StateMachine;
using SpiderController.TriggerChecker;
using SpiderController.UI;
using UnityEngine;

namespace SpiderController.PickUp
{
    public class EnergyPickup : ISavedProgressReader
    {
        private readonly IPersistentProgressService _persistentProgressService;
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly IXRayService _xRayService;
        private readonly IWindowService _windowService;
        private readonly SpiderStateContext _stateContext;
        private readonly EnergyChecker _energyChecker;
        private readonly EnergyLegs _energyLegs;
        private StateMachineData Data => _stateContext.Data;
        private EnergyBarUI EnergyBarUI => _stateContext.SpiderUI.EnergyBar;

        public EnergyPickup(
            IPersistentProgressService persistentProgressService,
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IXRayService xRayService,
            IWindowService windowService,
            SpiderStateContext stateContext,
            EnergyChecker energyChecker,
            EnergyLegs energyLegs)
        {
            _persistentProgressService = persistentProgressService;
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _xRayService = xRayService;
            _windowService = windowService;
            _stateContext = stateContext;
            _energyChecker = energyChecker;
            _energyLegs = energyLegs;
        }

        public void Initialize() =>
            _energyChecker.OnRemoveHappened += Hide;

        public void Destroy() =>
            _energyChecker.OnRemoveHappened -= Hide;

        public void LoadProgress(PlayerProgress progress)
        {
            List<EnergyData> energyDatas = progress.WorldProgressData.EnergyDatas;

            for (int i = 0; i < energyDatas.Count; i++)
            {
                Data.EnergyFillAmount++;
                _energyLegs.AddEnergyOnLeg();
                EnergyBarUI.AddNewSegment();
            }
        }

        public void Update()
        {
            if (_inputService.PickupPressed)
                PickUp();

            TryShow();
        }

        private void TryShow()
        {
            foreach (Collider collider in _energyChecker.Results)
                _pickupDisplayer.Show(collider.transform);
        }

        private void Hide(Collider obj) =>
            _pickupDisplayer.Hide(obj.transform);

        private void PickUp()
        {
            int count = _energyChecker.Results.Count;

            if (count == 0)
                return;

            _windowService.OpenProductDescriptionPopup(ProductType.Energy);

            for (int i = 0; i < count; i++)
            {
                Data.EnergyFillAmount++;
                _energyLegs.AddEnergyOnLeg();
                EnergyBarUI.AddNewSegment();

                Collider forDelete = _energyChecker.Results[i];

                MarkerUniqueId markerUniqueId = forDelete.GetComponent<MarkerUniqueId>();

                _persistentProgressService.PlayerProgress.WorldProgressData.EnergyDatas
                    .Add(new EnergyData(markerUniqueId.UniqueId));

                _pickupDisplayer.Hide(forDelete.transform);
                _xRayService.Remove(forDelete.GetComponent<XRayMarker>());

                Object.Destroy(forDelete.gameObject);
                _energyChecker.Results.Clear();
            }
        }
    }
}