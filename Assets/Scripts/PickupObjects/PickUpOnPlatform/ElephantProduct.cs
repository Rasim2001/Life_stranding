using Infastructure.StaticData.Product;
using Infastructure.StaticData.StaticDataService;
using SpiderController.Platform;
using SpiderController.StateMachine;
using UnityEngine;
using Zenject;

namespace PickupObjects.PickUpOnPlatform
{
    public class ElephantProduct : PickupObjectBase, IProduct
    {
        public ProductType ProductType { get; set; }

        private IStaticDataService _staticDataService;

        private StateMachineData _stateMachineData;
        private ProductData _productData;


        [Inject]
        public void Construct(IStaticDataService staticDataService) =>
            _staticDataService = staticDataService;

        public override void Initialize(Transform platformTransform, PlatformSelector platformSelector)
        {
            base.Initialize(platformTransform, platformSelector);

            _productData = _staticDataService.ProductsStaticData.ProductsDictionary[ProductType];
            Speed = _productData.Speed;
            StartPosition = _productData.StartPositionVector;
            StartRotation = Quaternion.Euler(_productData.StartRotationEuler);
        }

        public void Initialize(StateMachineData stateMachineData) =>
            _stateMachineData = stateMachineData;

        public override void StopSimulatePhysics()
        {
            base.StopSimulatePhysics();

            _stateMachineData.TotalWeight += _productData.Weight;
        }

        public override void StartSimulatePhysics()
        {
            base.StartSimulatePhysics();

            _stateMachineData.TotalWeight -= _productData.Weight;
        }
    }
}