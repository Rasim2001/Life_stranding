using System.Linq;
using Infastructure.Common.Pickup;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Infastructure.Services.XRay;
using Infastructure.StaticData.XRay;
using PickupObjects;
using SpiderController.TriggerChecker;
using UnityEngine;

namespace SpiderController.PickUp
{
    public class SkillProductPickup
    {
        private readonly IInputService _inputService;
        private readonly IPickupDisplayer _pickupDisplayer;
        private readonly IXRayService _xRayService;
        private readonly IWindowService _windowService;
        private readonly SkillProductChecker _skillChecker;

        public SkillProductPickup(
            IInputService inputService,
            IPickupDisplayer pickupDisplayer,
            IXRayService xRayService,
            IWindowService windowService,
            SkillProductChecker skillChecker)
        {
            _inputService = inputService;
            _pickupDisplayer = pickupDisplayer;
            _xRayService = xRayService;
            _windowService = windowService;
            _skillChecker = skillChecker;
        }

        public void Initialize() =>
            _skillChecker.OnRemoveHappened += Hide;

        public void Destroy() =>
            _skillChecker.OnRemoveHappened -= Hide;

        public void Update()
        {
            if (_inputService.PickupPressed)
                PickUp();

            TryShow();
        }

        private void TryShow()
        {
            foreach (Collider collider in _skillChecker.Results)
                _pickupDisplayer.Show(collider.transform);
        }

        private void Hide(Collider obj) =>
            _pickupDisplayer.Hide(obj.transform);

        private void PickUp()
        {
            Collider skillCollider = _skillChecker.Results.FirstOrDefault();

            if (skillCollider == null)
                return;

            IProduct product = skillCollider.GetComponent<IProduct>();

            _windowService.OpenProductDescriptionPopup(product.ProductType);

            _pickupDisplayer.Hide(skillCollider.transform);
            _xRayService.Remove(skillCollider.GetComponent<XRayMarker>());

            Object.Destroy(skillCollider.gameObject);
            _skillChecker.Results.Clear();
        }
    }
}