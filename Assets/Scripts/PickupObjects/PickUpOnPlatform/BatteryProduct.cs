using Infastructure.Services.XRay;
using Infastructure.StaticData.Product;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.XRay;
using SpiderController.Platform;
using UnityEngine;
using Zenject;

namespace PickupObjects.PickUpOnPlatform
{
    public class BatteryProduct : PickupObjectBase, IProduct
    {
        public ProductType ProductType { get; set; }

        private XRayMarker _xRayMarker;
        private IXRayService _xRayService;
        private IStaticDataService _staticDataService;

        [Inject]
        public void Construct(IXRayService xRayService, IStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;
            _xRayService = xRayService;
        }

        public override void Initialize(Transform platformTransform, PlatformSelector platformSelector)
        {
            base.Initialize(platformTransform, platformSelector);

            ProductData productData = _staticDataService.ProductsStaticData.ProductsDictionary[ProductType];
            Speed = productData.Speed;
            StartPosition = productData.StartPositionVector;
            StartRotation = Quaternion.Euler(productData.StartRotationEuler);
        }


        private void Start() =>
            _xRayMarker = GetComponent<XRayMarker>();

        public override void StopSimulatePhysics()
        {
            base.StopSimulatePhysics();

            _xRayService.Remove(_xRayMarker);
        }

        protected override void StartSimulatePhysics()
        {
            base.StartSimulatePhysics();

            _xRayService.Add(_xRayMarker);
        }
    }
}