using System.Collections.Generic;
using System.Linq;
using Common;
using Infastructure.Data;
using Infastructure.Services.SaveLoadService;
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
    public class BatteryProduct : PickupObjectBase, IProduct, ISavedProgress
    {
        public ProductType ProductType { get; set; }

        private XRayMarker _xRayMarker;
        private IXRayService _xRayService;
        private IStaticDataService _staticDataService;

        private ProductData _productData;
        private StateMachineData _stateMachineData;

        private MarkerUniqueId _markerUniqueId;

        [Inject]
        public void Construct(IXRayService xRayService, IStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;
            _xRayService = xRayService;
        }

        protected override void Awake()
        {
            base.Awake();

            _markerUniqueId = GetComponent<MarkerUniqueId>();
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

        public void LoadProgress(PlayerProgress progress)
        {
            BatteryProductData batteryProductData =
                progress.WorldProgressData.BatteryProductDatas.FirstOrDefault(x =>
                    x.UniqueId == _markerUniqueId.UniqueId);

            if (batteryProductData == null)
                return;

            if (batteryProductData.IsPuttingDown)
            {
                Collider.enabled = false;
                Rigidbody.isKinematic = true;
            }


            transform.position = batteryProductData.Position.AsUnityVector();
            transform.localEulerAngles = batteryProductData.Rotation.AsUnityVector();
        }

        public void UpdateProgress(PlayerProgress progress)
        {
            List<BatteryProductData> list = progress.WorldProgressData.BatteryProductDatas;

            BatteryProductData existing = list.FirstOrDefault(x => x.UniqueId == _markerUniqueId.UniqueId);

            if (existing == null)
            {
                list.Add(new BatteryProductData(
                    transform.position.AsVectorData(),
                    transform.localEulerAngles.AsVectorData(),
                    _markerUniqueId.UniqueId, IsPuttingDown));
            }
            else
            {
                existing.Position = transform.position.AsVectorData();
                existing.Rotation = transform.localEulerAngles.AsVectorData();
            }
        }


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
            Rigidbody.isKinematic = true;

            _stateMachineData.TotalWeight -= _productData.Weight;

            base.StartSimulatePhysics();
        }
    }
}