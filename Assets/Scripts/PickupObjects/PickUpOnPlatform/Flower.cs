using System;
using CheckPointManagement;
using HUD;
using Infastructure.StaticData.Product;
using Infastructure.StaticData.StaticDataService;
using MoreMountains.Feedbacks;
using SpiderController.Platform;
using UnityEngine;
using Zenject;

namespace PickupObjects.PickUpOnPlatform
{
    public class Flower : PickupObjectBase, IProduct
    {
        [SerializeField] private MMF_Player _feedbackPlayer;
        [SerializeField] private LayerMask _groundLayer;

        public ProductType ProductType { get; set; }

        public Action OnDroppedFromPlatform;
        public Action OnGroundTriggered;

        private FlowerPointIndicator _flowerPointIndicator;
        private IStaticDataService _staticDataService;

        private bool _isTriggered;

        [Inject]
        public void Construct(IStaticDataService staticDataService) =>
            _staticDataService = staticDataService;


        public void Initialize(FlowerPointIndicator flowerPointIndicator) =>
            _flowerPointIndicator = flowerPointIndicator;

        public override void Initialize(Transform platformTransform, PlatformSelector platformSelector)
        {
            base.Initialize(platformTransform, platformSelector);

            ProductData productData = _staticDataService.ProductsStaticData.ProductsDictionary[ProductType];
            Speed = productData.Speed;
            StartPosition = productData.StartPositionVector;
            StartRotation = Quaternion.Euler(productData.StartRotationEuler);
        }

        protected override void OnCollisionEnter(Collision other)
        {
            base.OnCollisionEnter(other);

            if (_isTriggered || _groundLayer != (_groundLayer | (1 << other.gameObject.layer)))
                return;

            _isTriggered = true;
            OnGroundTriggered?.Invoke();
        }


        public override void StopSimulatePhysics()
        {
            base.StopSimulatePhysics();

            _isTriggered = false;
            _flowerPointIndicator.HideTargetPoint();
        }

        public override void StartSimulatePhysics()
        {
            base.StartSimulatePhysics();

            _feedbackPlayer.PlayFeedbacks();
            _flowerPointIndicator.ShowTargetPoint();

            OnDroppedFromPlatform?.Invoke();
        }

        public void Putdown(CheckPoint checkPoint)
        {
            Rigidbody.isKinematic = true;
            IsPuttingDown = true;
            Collider.enabled = false;

            transform.position = checkPoint.FlowerPutdownPosition;
            transform.rotation = checkPoint.FlowerPutdownRotation;

            PlatformSelector.IsOnPlatform(Collider);

            StartSimulatePhysics();
        }

        public void PickUpAfterPutdown()
        {
            IsPuttingDown = false;
            Rigidbody.isKinematic = false;
            Collider.enabled = true;

            StopSimulatePhysics();
        }
    }
}