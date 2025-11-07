using System;
using Common;
using HUD;
using Infastructure.StaticData.Product;
using Infastructure.StaticData.StaticDataService;
using MoreMountains.Feedbacks;
using SpiderController.Platform;
using SpiderController.StateMachine;
using UnityEngine;
using Zenject;

namespace PickupObjects.PickUpOnPlatform.FlowerManagement
{
    public class Flower : PickupObjectBase, IProduct
    {
        [SerializeField] private MMF_Player _feedbackPlayer;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private GameObject[] _flowerVariants;

        public ProductType ProductType { get; set; }

        public Action OnDroppedFromPlatform;
        public Action OnGroundTriggered;

        private FlowerPointIndicator _flowerPointIndicator;
        private FlowerSelector _flowerSelector;
        private StateMachineData _stateMachineData;
        private ProductData _productData;

        private IStaticDataService _staticDataService;

        private bool _isTriggered;

        [Inject]
        public void Construct(IStaticDataService staticDataService) =>
            _staticDataService = staticDataService;

        protected override void Awake()
        {
            base.Awake();

            _flowerSelector = new FlowerSelector(_flowerVariants);
            _flowerSelector.Initialize();
        }

        private void OnDestroy() =>
            _flowerSelector.Clear();

        public void ResetFlowerVariant() =>
            _flowerSelector.Reset();

        public void Initialize(FlowerPointIndicator flowerPointIndicator, StateMachineData stateMachineData)
        {
            _stateMachineData = stateMachineData;
            _flowerPointIndicator = flowerPointIndicator;
        }

        public override void Initialize(Transform platformTransform, PlatformSelector platformSelector)
        {
            base.Initialize(platformTransform, platformSelector);

            _productData = _staticDataService.ProductsStaticData.ProductsDictionary[ProductType];
            Speed = _productData.Speed;
            StartPosition = _productData.StartPositionVector;
            StartRotation = Quaternion.Euler(_productData.StartRotationEuler);
        }

        protected override void OnCollisionEnter(Collision other)
        {
            base.OnCollisionEnter(other);

            if (_isTriggered || _groundLayer != (_groundLayer | (1 << other.gameObject.layer)))
                return;

            _flowerSelector.ShowNextVariant();

            _isTriggered = true;
            OnGroundTriggered?.Invoke();
        }


        public override void StopSimulatePhysics()
        {
            base.StopSimulatePhysics();

            _isTriggered = false;
            _flowerPointIndicator.HideTargetPoint();

            _stateMachineData.TotalWeight += _productData.Weight;
        }

        public override void StartSimulatePhysics()
        {
            base.StartSimulatePhysics();

            _feedbackPlayer.PlayFeedbacks();
            _flowerPointIndicator.ShowTargetPoint();

            _stateMachineData.TotalWeight -= _productData.Weight;

            OnDroppedFromPlatform?.Invoke();
        }

        public void Putdown(CheckPoint checkPoint)
        {
            Rigidbody.isKinematic = true;
            IsPuttingDown = true;
            Collider.enabled = false;

            transform.position = checkPoint.FlowerPutdownPosition;
            transform.rotation = checkPoint.FlowerPutdownRotation;

            _stateMachineData.TotalWeight -= _productData.Weight;

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