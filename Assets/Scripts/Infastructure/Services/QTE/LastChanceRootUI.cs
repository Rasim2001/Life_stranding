using System;
using DG.Tweening;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.Pause;
using Infastructure.StaticData.LastChance;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Infastructure.Services.QTE
{
    public class LastChanceRootUI : MonoBehaviour
    {
        [SerializeField] private Transform _container;
        [SerializeField] private Transform _ringBG;
        [SerializeField] private Image _iconImage;

        [SerializeField] private float _ringShrinkTargetScale;
        private LastChanceStaticData LastChanceStaticData => _staticDataService.LastChanceStaticData;

        private Flower _flower;
        private Tween _shrinkRingTween;
        private Tweener _shakeTween;

        private IStaticDataService _staticDataService;
        private ICameraProviderService _cameraProviderService;
        private IPauseService _pauseService;

        [Inject]
        public void Construct(IStaticDataService staticDataService, ICameraProviderService cameraProviderService,
            IPauseService pauseService)
        {
            _pauseService = pauseService;
            _cameraProviderService = cameraProviderService;
            _staticDataService = staticDataService;
        }

        private void Start()
        {
            _pauseService.OnPauseChanged += PauseChanged;

            Clear();
        }

        private void OnDestroy()
        {
            _pauseService.OnPauseChanged -= PauseChanged;

            Clear();
        }

        public void SetFlowerTransform(Flower flower) =>
            _flower = flower;

        public void PickUpFlower() =>
            _flower.StopSimulatePhysics();

        public void Show(Action OnShrinkRingFinishHappened)
        {
            Vector3 screenPos = _cameraProviderService.Camera.WorldToScreenPoint(_flower.transform.position);
            _container.position = screenPos;

            _container.gameObject.SetActive(true);

            ShrinkingRing(OnShrinkRingFinishHappened);
        }

        public Tween ShakeIcon() =>
            _shakeTween = _iconImage.transform
                .DOShakePosition(duration: 0.2f,
                    strength: new Vector3(10f, 10f, 0f),
                    vibrato: 100,
                    randomness: 90f,
                    snapping: false,
                    fadeOut: true)
                .SetUpdate(true);

        public void Clear()
        {
            _container.gameObject.SetActive(false);

            _iconImage.sprite = LastChanceStaticData.DeselectedSprite;
            _ringBG.localScale = Vector3.one;

            _shrinkRingTween?.Kill();
            _shakeTween?.Kill();
        }

        public void ChangeSelectedSprite() =>
            _iconImage.sprite = LastChanceStaticData.SelectedSprite;

        public void ChangeDeSelectedSprite() =>
            _iconImage.sprite = LastChanceStaticData.DeselectedSprite;


        private void PauseChanged()
        {
            if (_pauseService.IsPaused)
            {
                Debug.Log("Paused");

                _shrinkRingTween?.Pause();
            }
            else
            {
                Debug.Log("Playing");

                _shrinkRingTween?.Play();
            }
        }

        private void ShrinkingRing(Action OnHappened)
        {
            _shrinkRingTween = _ringBG
                .DOScale(new Vector3(_ringShrinkTargetScale, _ringShrinkTargetScale, 0),
                    LastChanceStaticData.ShrinkingDuration)
                .SetUpdate(true)
                .SetEase(Ease.Linear)
                .OnComplete(() => OnHappened?.Invoke());
        }
    }
}