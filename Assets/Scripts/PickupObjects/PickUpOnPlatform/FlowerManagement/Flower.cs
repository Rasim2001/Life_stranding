using System;
using System.Collections;
using Common;
using HighlightPlus;
using HUD;
using Infastructure.Data;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.QTE;
using Infastructure.Services.SaveLoadService;
using Infastructure.Services.SlowTime;
using Infastructure.Services.XRay;
using Infastructure.StaticData.XRay;
using PickupObjects.Rewind;
using UnityEngine;
using Zenject;

namespace PickupObjects.PickUpOnPlatform.FlowerManagement
{
    public class Flower : PickupObjectBase, ISavedProgress
    {
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private GameObject[] _flowerVariants;
        [SerializeField] private HighlightEffect _outlineEffect;

        public event Action OnDroppedFromPlatform;
        public event Action OnGroundTriggered;

        private FlowerPointIndicator _flowerPointIndicator;
        private FlowerSelector _flowerSelector;
        private XRayMarker _xRayMarker;

        private ILastChanceQTEService _lastChanceQteService;
        private IXRayService _xRayService;
        private ISlowTimeRunner _slowTimeRunner;

        private bool _isTriggered;
        private Coroutine _outlineCoroutine;

        [Inject]
        public void Construct(
            ILastChanceQTEService lastChanceQteService,
            IXRayService xRayService,
            ISlowTimeRunner slowTimeRunner)
        {
            _slowTimeRunner = slowTimeRunner;
            _xRayService = xRayService;
            _lastChanceQteService = lastChanceQteService;
        }

        protected override void Awake()
        {
            base.Awake();

            _xRayMarker = GetComponent<XRayMarker>();
            _flowerSelector = new FlowerSelector(_flowerVariants);
            _flowerSelector.Initialize();
        }

        public void Initialize(FlowerPointIndicator flowerPointIndicator) =>
            _flowerPointIndicator = flowerPointIndicator;

        public void LoadProgress(PlayerProgress progress)
        {
            if (progress.WorldProgressData.FlowerData.Position == null)
                return;

            if (progress.WorldProgressData.FlowerData.IsOnPlatform)
            {
                PlatformObjectsService.Add(this);
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

        private void OnDestroy()
        {
            StopOutlineCoroutine();
            _flowerSelector.Clear();
        }

        public override void OnBlinkZoneStateChanged(bool isInside)
        {
            if (isInside)
                StopOutlineCoroutine();
            else
                StartOutlineCoroutine();
        }


        public override void ThrowObject()
        {
            base.ThrowObject();

            _flowerPointIndicator.ShowTargetPoint();
            OnDroppedFromPlatform?.Invoke();
            _xRayService.Add(_xRayMarker);

            StopOutlineCoroutine();
        }

        public override void ApplyFinalSnapshot(PickupObjectSnapshot snapshot)
        {
            base.ApplyFinalSnapshot(snapshot);

            if (IsOnPlatform)
            {
                _isTriggered = false;
                _flowerPointIndicator.HideTargetPoint();
                _xRayService.Remove(_xRayMarker);
            }
            else
            {
                _flowerPointIndicator.ShowTargetPoint();
                _xRayService.Add(_xRayMarker);
            }
        }

        public override void AttachToPlatform(Transform platformTransform)
        {
            base.AttachToPlatform(platformTransform);

            _isTriggered = false;
            _flowerPointIndicator.HideTargetPoint();
            _xRayService.Remove(_xRayMarker);
        }

        public override void DetachFromPlatform()
        {
            base.DetachFromPlatform();

            StopOutlineCoroutine();
        }

        public override void GetChanceAttachToPlatform()
        {
            base.GetChanceAttachToPlatform();

            _lastChanceQteService.StartQTE();
            _slowTimeRunner.SlowDown();
            _flowerPointIndicator.ShowTargetPoint();

            OnDroppedFromPlatform?.Invoke();
            _xRayService.Add(_xRayMarker);
        }


        protected override void OnCollisionEnter(Collision other)
        {
            base.OnCollisionEnter(other);

            if (_isTriggered || !WasOnPlatform)
                return;

            if (_groundLayer != (_groundLayer | (1 << other.gameObject.layer)))
                return;

            _flowerSelector.ShowNextVariant();

            _isTriggered = true;
            OnGroundTriggered?.Invoke();
        }

        public void Putdown(ICheckpointInfo checkPoint)
        {
            IsPuttingDown = true;
            Collider.enabled = false;

            transform.position = checkPoint.FlowerPutdownPosition;
            transform.rotation = checkPoint.FlowerPutdownRotation;

            Rigidbody.isKinematic = true;

            StopOutlineCoroutine();
        }

        public void PickUpAfterPutdown()
        {
            IsPuttingDown = false;
            Rigidbody.isKinematic = false;
            Collider.enabled = true;
        }


        public void ResetFlowerVariant() =>
            _flowerSelector.Reset();

        private void StopOutlineCoroutine()
        {
            if (_outlineCoroutine != null)
            {
                StopCoroutine(_outlineCoroutine);
                _outlineCoroutine = null;

                _outlineEffect.SetHighlighted(false);
            }
        }

        private void StartOutlineCoroutine()
        {
            if (_outlineCoroutine != null)
                return;

            _outlineCoroutine = StartCoroutine(OutlineCoroutineAnimation());
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
    }
}