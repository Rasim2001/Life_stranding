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
        private readonly EnergyChecker _energyChecker;
        private readonly EnergyBarUI _energyBarUI;
        private readonly StateMachineData _data;
        private readonly EnergyLegs _energyLegs;

        public EnergyPickup(
            IPersistentProgressService persistentProgressService,
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IXRayService xRayService,
            IWindowService windowService,
            EnergyChecker energyChecker,
            EnergyBarUI energyBarUI,
            StateMachineData data,
            EnergyLegs energyLegs)
        {
            _persistentProgressService = persistentProgressService;
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _xRayService = xRayService;
            _windowService = windowService;
            _energyChecker = energyChecker;
            _energyBarUI = energyBarUI;
            _data = data;
            _energyLegs = energyLegs;
        }

        public void Initialize() =>
            _energyChecker.OnRemoveHappened += Hide;

        public void LoadProgress(PlayerProgress progress)
        {
            List<EnergyData> energyDatas = progress.WorldProgressData.EnergyDatas;

            for (int i = 0; i < energyDatas.Count; i++)
            {
                _data.EnergyFillAmount++;
                _energyLegs.AddEnergyOnLeg();
                _energyBarUI.AddNewSegment();
            }
        }

        public void Destroy() =>
            _energyChecker.OnRemoveHappened -= Hide;

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
                _data.EnergyFillAmount++;
                _energyLegs.AddEnergyOnLeg();
                _energyBarUI.AddNewSegment();

                Collider forDelete = _energyChecker.Results[i];

                MarkerUniqueId markerUniqueId = forDelete.GetComponent<MarkerUniqueId>();

                _persistentProgressService.PlayerProgress.WorldProgressData.EnergyDatas.Add(
                    new EnergyData(markerUniqueId.UniqueId));

                _pickupDisplayer.Hide(forDelete.transform);
                _xRayService.Remove(forDelete.GetComponent<XRayMarker>());

                Object.Destroy(forDelete.gameObject);
                _energyChecker.Results.Clear();
            }
        }
    }
}