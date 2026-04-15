using Common;
using Infastructure.Services.XRay;
using Infastructure.StaticData.Product;
using Infastructure.StaticData.XRay;
using UnityEngine;
using Zenject;

namespace PickupObjects.PickUpOnPlatform
{
    public class ElephantProduct : PickupObjectBase
    {
        private XRayMarker _xRayMarker;
        private IXRayService _xRayService;

        [Inject]
        public void Construct(IXRayService xRayService) =>
            _xRayService = xRayService;


        public override void ThrowObject() 
        {
            base.ThrowObject();

            //_xRayService.Add(_xRayMarker);
        }

        public override void AttachToPlatform(Transform platformTransform)
        {
            base.AttachToPlatform(platformTransform);

            //_xRayService.Remove(_xRayMarker);
        }

        public override void DetachFromPlatform()
        {
            base.DetachFromPlatform();

            //_xRayService.Add(_xRayMarker);
        }
    }
}