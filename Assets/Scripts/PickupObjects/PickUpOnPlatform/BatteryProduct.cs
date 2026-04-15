using System.Collections.Generic;
using System.Linq;
using Common;
using Infastructure.Data;
using Infastructure.Services.SaveLoadService;
using Infastructure.Services.XRay;
using Infastructure.StaticData.Product;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.XRay;
using SpiderController.StateMachine;
using UnityEngine;
using Zenject;

namespace PickupObjects.PickUpOnPlatform
{
    public class BatteryProduct : PickupObjectBase, ISavedProgress
    {
        private XRayMarker _xRayMarker;
        private IXRayService _xRayService;

        private ProductData _productData;

        private MarkerUniqueId _markerUniqueId;

        [Inject]
        public void Construct(IXRayService xRayService) =>
            _xRayService = xRayService;

        protected override void Awake()
        {
            base.Awake();

            _markerUniqueId = GetComponent<MarkerUniqueId>();
        }

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

        public override void ThrowObject()
        {
            base.ThrowObject();

            _xRayService.Add(_xRayMarker);
        }

        public override void AttachToPlatform(Transform platformTransform)
        {
            base.AttachToPlatform(platformTransform);

            _xRayService.Remove(_xRayMarker);
        }

        public override void DetachFromPlatform()
        {
            base.DetachFromPlatform();

            _xRayService.Add(_xRayMarker);
        }

        public void PutdownOnGenerator(Generator generator)
        {
            transform.position = generator.PutdownBatteryPoint.position;
            transform.rotation = generator.PutdownBatteryPoint.rotation;

            IsPuttingDown = true;
            Collider.enabled = false;

            //PlatformSelector.IsOnPlatform(Collider);
            Rigidbody.isKinematic = true;
        }
    }
}