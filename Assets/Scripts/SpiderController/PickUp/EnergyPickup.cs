using Infastructure.Common.Pickup;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.XRay;
using Infastructure.StaticData.XRay;
using SpiderController.StateMachine;
using SpiderController.TriggerChecker;
using SpiderController.UI;
using UnityEngine;

namespace SpiderController.PickUp
{
    public class EnergyPickup
    {
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly IXRayService _xRayService;
        private readonly EnergyChecker _energyChecker;
        private readonly EnergyBarUI _energyBarUI;
        private readonly StateMachineData _data;
        private readonly EnergyLegs _energyLegs;

        public EnergyPickup(
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IXRayService xRayService,
            EnergyChecker energyChecker,
            EnergyBarUI energyBarUI,
            StateMachineData data,
            EnergyLegs energyLegs)
        {
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _xRayService = xRayService;
            _energyChecker = energyChecker;
            _energyBarUI = energyBarUI;
            _data = data;
            _energyLegs = energyLegs;
        }

        public void Initialize() =>
            _energyChecker.OnRemoveHappened += Hide;

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

            for (int i = 0; i < count; i++)
            {
                _data.EnergyFillAmount++;
                _energyLegs.AddEnergyOnLeg();
                _energyBarUI.AddNewSegment();

                Collider forDelete = _energyChecker.Results[i];

                _pickupDisplayer.Hide(forDelete.transform);
                _xRayService.Remove(forDelete.GetComponent<XRayMarker>());

                Object.Destroy(forDelete.gameObject);
                _energyChecker.Results.Clear();
            }
        }
    }
}