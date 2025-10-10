using Infastructure.Services.XRay;
using Infastructure.StaticData.XRay;
using UnityEngine;
using Zenject;

namespace PickupObjects.PickUpOnPlatform
{
    public class BatteryProduct : PickupObjectBase, IProduct
    {
        public ProductType ProductType { get; set; }

        private XRayMarker _xRayMarker;
        private IXRayService _xRayService;

        [Inject]
        public void Construct(IXRayService xRayService) =>
            _xRayService = xRayService;

        private void Start() =>
            _xRayMarker = GetComponent<XRayMarker>();

        protected override void StartSimulatePhysics()
        {
            base.StartSimulatePhysics();

            _xRayService.Add(_xRayMarker);
        }

        public override void StopSimulatePhysics()
        {
            base.StopSimulatePhysics();

            _xRayService.Remove(_xRayMarker);
        }
    }
}