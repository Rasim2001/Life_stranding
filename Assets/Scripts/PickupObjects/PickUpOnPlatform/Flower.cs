using System;
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
        public ProductType ProductType { get; set; }

        public Action OnDroppedFromPlatform;

        private FlowerPointIndicator _flowerPointIndicator;
        private IStaticDataService _staticDataService;

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

        public override void StopSimulatePhysics()
        {
            base.StopSimulatePhysics();

            _flowerPointIndicator.HideTargetPoint();
        }

        public override void StartSimulatePhysics()
        {
            base.StartSimulatePhysics();

            _feedbackPlayer.PlayFeedbacks();
            _flowerPointIndicator.ShowTargetPoint();

            OnDroppedFromPlatform?.Invoke();
        }
    }
}