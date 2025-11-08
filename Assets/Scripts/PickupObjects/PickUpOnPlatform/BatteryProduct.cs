using Common;
using Infastructure.Services.XRay;
using Infastructure.StaticData.Product;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.XRay;
using SpiderController.Platform;
using SpiderController.StateMachine;
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

        private ProductData _productData;
        private StateMachineData _stateMachineData;

        [Inject]
        public void Construct(IXRayService xRayService, IStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;
            _xRayService = xRayService;
        }

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


        private void Start() =>
            _xRayMarker = GetComponent<XRayMarker>();

        public override void StopSimulatePhysics()
        {
            if (!IsOnPlatform)
                _stateMachineData.TotalWeight += _productData.Weight;

            base.StopSimulatePhysics();

            _xRayService.Remove(_xRayMarker);
        }

        public override void StartSimulatePhysics()
        {
            base.StartSimulatePhysics();

            _stateMachineData.TotalWeight -= _productData.Weight;

            _xRayService.Add(_xRayMarker);
        }

        public void PutdownOnGenerator(Generator generator)
        {
            transform.position = generator.PutdownBatteryPoint.position;
            transform.rotation = generator.PutdownBatteryPoint.rotation;

            IsPuttingDown = true;
            Collider.enabled = false;

            PlatformSelector.IsOnPlatform(Collider);

            base.StartSimulatePhysics();

            Rigidbody.isKinematic = true;
        }
    }
}