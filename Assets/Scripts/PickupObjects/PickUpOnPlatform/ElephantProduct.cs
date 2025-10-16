using Infastructure.StaticData.Product;
using Infastructure.StaticData.StaticDataService;
using SpiderController.Platform;
using UnityEngine;
using Zenject;

namespace PickupObjects.PickUpOnPlatform
{
    public class ElephantProduct : PickupObjectBase, IProduct
    {
        private IStaticDataService _staticDataService;
        public ProductType ProductType { get; set; }

        [Inject]
        public void Construct(IStaticDataService staticDataService) => 
            _staticDataService = staticDataService;

        public override void Initialize(Transform platformTransform, PlatformSelector platformSelector)
        {
            base.Initialize(platformTransform, platformSelector);

            ProductData productData = _staticDataService.ProductsStaticData.ProductsDictionary[ProductType];
            Speed = productData.Speed;
            StartPosition = productData.StartPositionVector;
            StartRotation = Quaternion.Euler(productData.StartRotationEuler);
        }
    }
}