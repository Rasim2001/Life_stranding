using System;
using System.Collections;
using Common;
using HighlightPlus;
using HUD;
using Infastructure.Data;
using Infastructure.Services.QTE;
using Infastructure.Services.SaveLoadService;
using Infastructure.Services.SlowTime;
using Infastructure.Services.XRay;
using Infastructure.StaticData.Product;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.XRay;
using SpiderController.Platform;
using SpiderController.StateMachine;
using Unity.Burst.CompilerServices;
using UnityEngine;
using Zenject;

namespace PickupObjects.PickUpOnPlatform.FlowerManagement
{
    public class Flower : PickupObjectBase, IProduct, ISavedProgress
    {
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private GameObject[] _flowerVariants;
        [SerializeField] private HighlightEffect _outlineEffect;

        public ProductType ProductType { get; set; }

        public Action OnDroppedFromPlatform;
        public Action OnGroundTriggered;

        private FlowerPointIndicator _flowerPointIndicator;
        private FlowerSelector _flowerSelector;
        private StateMachineData _stateMachineData;
        private ProductData _productData;
        private XRayMarker _xRayMarker;

        private IStaticDataService _staticDataService;
        private ILastChanceQTEService _lastChanceQteService;

        private bool _isTriggered;
        private IXRayService _xRayService;
        private ISlowTimeRunner _slowTimeRunner;

        private bool _isInside;
        private Coroutine _outlineCoroutine;

        [Inject]
        public void Construct(IStaticDataService staticDataService, ILastChanceQTEService lastChanceQteService,
            IXRayService xRayService, ISlowTimeRunner slowTimeRunner)
        {
            _slowTimeRunner = slowTimeRunner;
            _xRayService = xRayService;
            _lastChanceQteService = lastChanceQteService;
            _staticDataService = staticDataService;
        }

        protected override void Awake()
        {
            base.Awake();

            GetComponent<HighlightEffect>();
            _xRayMarker = GetComponent<XRayMarker>();
            _flowerSelector = new FlowerSelector(_flowerVariants);
            _flowerSelector.Initialize();
        }

        public void LoadProgress(PlayerProgress progress)
        {
            if (progress.WorldProgressData.FlowerData.Position == null)
                return;

            if (progress.WorldProgressData.FlowerData.IsOnPlatform)
            {
                StopSimulatePhysics();

                return;
            }

            transform.position = progress.WorldProgressData.FlowerData.Position.AsUnityVector();
            transform.localEulerAngles = progress.WorldProgressData.FlowerData.Rotation.AsUnityVector();
            IsPuttingDown = progress.WorldProgressData.FlowerData.IsPuttingDown;

            if (IsPuttingDown)
            {
                Collider.enabled = false;
                Rigidbody.isKinematic = true;
            }
        }

        public void UpdateProgress(PlayerProgress progress)
        {
            progress.WorldProgressData.FlowerData.Position = transform.position.AsVectorData();
            progress.WorldProgressData.FlowerData.Rotation = transform.localEulerAngles.AsVectorData();
            progress.WorldProgressData.FlowerData.IsPuttingDown = IsPuttingDown;
            progress.WorldProgressData.FlowerData.IsOnPlatform = IsOnPlatform;
        }

        protected override void Update()
        {
            base.Update();

            if (!IsOnPlatform)
                return;

            if (PlatformSelector.IsInsideOfBlinkPlace(Collider))
            {
                StopCoroutine();

                _outlineEffect.SetHighlighted(false);
            }
            else
                _outlineCoroutine ??= StartCoroutine(OutlineCoroutineAnimation());
        }


        private void OnDestroy()
        {
            StopCoroutine();

            _flowerSelector.Clear();
        }

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


        public override void ThrowObject()
        {
            base.ThrowObject();

            _flowerPointIndicator.ShowTargetPoint();
            _stateMachineData.TotalWeight -= _productData.Weight;

            OnDroppedFromPlatform?.Invoke();

            _xRayService.Add(_xRayMarker);
        }

        protected override void OnCollisionEnter(Collision other)
        {
            base.OnCollisionEnter(other);

            if (_isTriggered || _groundLayer != (_groundLayer | (1 << other.gameObject.layer)) || !WasOnPlatform)
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

            _xRayService.Remove(_xRayMarker);
        }

        public override void StartSimulatePhysics()
        {
            base.StartSimulatePhysics();

            _lastChanceQteService.StartQTE();
            _slowTimeRunner.SlowDown();
            _flowerPointIndicator.ShowTargetPoint();

            _stateMachineData.TotalWeight -= _productData.Weight;

            OnDroppedFromPlatform?.Invoke();

            _xRayService.Add(_xRayMarker);
        }


        public void Putdown(ICheckpointInfo checkPoint)
        {
            IsPuttingDown = true;
            Collider.enabled = false;

            transform.position = checkPoint.FlowerPutdownPosition;
            transform.rotation = checkPoint.FlowerPutdownRotation;

            _stateMachineData.TotalWeight -= _productData.Weight;

            PlatformSelector.IsOnPlatform(Collider);

            StartSimulatePhysicsAfterPutdown();

            Rigidbody.isKinematic = true;
        }

        public void PickUpAfterPutdown()
        {
            IsPuttingDown = false;
            Rigidbody.isKinematic = false;
            Collider.enabled = true;

            StopSimulatePhysics();
        }


        private void StopCoroutine()
        {
            if (_outlineCoroutine != null)
            {
                StopCoroutine(_outlineCoroutine);
                _outlineCoroutine = null;
            }
        }

        private IEnumerator OutlineCoroutineAnimation()
        {
            while (true)
            {
                _outlineEffect.SetHighlighted(true);

                yield return new WaitForSeconds(0.1f);

                _outlineEffect.SetHighlighted(false);

                yield return new WaitForSeconds(0.1f);
            }
        }

        private void StartSimulatePhysicsAfterPutdown()
        {
            base.StartSimulatePhysics();

            _flowerPointIndicator.ShowTargetPoint();

            _stateMachineData.TotalWeight -= _productData.Weight;

            OnDroppedFromPlatform?.Invoke();
        }
    }
}