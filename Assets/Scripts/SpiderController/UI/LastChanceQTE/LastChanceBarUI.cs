using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace SpiderController.UI.LastChanceQTE
{
    public class LastChanceBarUI : BarBaseUI
    {
        [SerializeField] private Transform _container;
        [SerializeField] private TextMeshProUGUI _amountText;

        private HologramEffect _hologramEffect;
        private Color _colorDefault;

        private Sequence _sequence;
        private Tweener _shakeTween;

        private void Awake()
        {
            _colorDefault = _amountText.color;

            _hologramEffect = new HologramEffect(GetSegments(), GetContainers(), GetOtherObjects());
        }

        protected override void Start()
        {
            base.Start();

            _hologramEffect.OnHologramEffectStartHappened += HideText;

            PlayFadeHologramEffect();
            HideText();
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
            _shakeTween?.Kill();

            _hologramEffect.OnHologramEffectStartHappened -= HideText;
            _hologramEffect.Clear();
        }

        public void PlayFadeHologramEffect() =>
            _hologramEffect.Play();

        public void ShowHologram() =>
            _hologramEffect.Stop();

        [Button]
        public void ShakeUI()
        {
            _shakeTween?.Kill();
            _amountText.color = _colorDefault;

            _shakeTween = _container
                .DOShakePosition(duration: 0.5f,
                    strength: new Vector3(0.05f, 0.05f, 0.05f),
                    vibrato: 200,
                    randomness: 90f,
                    snapping: false,
                    fadeOut: true)
                .SetUpdate(true)
                .SetDelay(0.25f);
        }

        public void UpdateSaveAttempts(int amount)
        {
            _sequence?.Kill();
            _sequence = DOTween.Sequence();
            _sequence
                .Append(_amountText.transform.DOScale(2f, 0.25f))
                .Append(_amountText.transform.DOScale(1f, 0.25f))
                .SetUpdate(true)
                .OnKill(() => _amountText.transform.localScale = Vector3.one)
                .SetDelay(0.25f);

            _amountText.color = _colorDefault;
            _amountText.text = amount.ToString();
        }


        private void HideText()
        {
            Color newColor = _colorDefault;
            newColor.a = 0;
            _amountText.color = newColor;
        }
    }
}