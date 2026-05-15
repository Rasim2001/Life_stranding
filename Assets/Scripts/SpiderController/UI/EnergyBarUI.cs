using Common.Lock;
using UnityEngine;
using UnityEngine.UI;

namespace SpiderController.UI
{
    public class EnergyBarUI : BarBaseUI
    {
        private const int FirstElement = 0;

        [SerializeField] private Color _firstIncreaseColor;
        [SerializeField] private Color _firstReducedColor;
        [SerializeField] private RectTransform _darkGlowRectTransform;

        private readonly float _stepWidth = 0.2f;

        private LockHandler _lockHandler = new LockHandler();

        private HologramEffect _hologramEffect;
        private Image[] _segmentsOwn;

        private bool _isLocked;

        protected override void Awake()
        {
            base.Awake();

            _segmentsOwn = GetSegments();

            _hologramEffect = new HologramEffect(_segmentsOwn, GetContainers(), GetOtherObjects());
        }

        public override void AddNewSegment()
        {
            base.AddNewSegment();

            UpdateGlowDarkWidth();
            ShowHologram();
        }

        public void PlayFadeHologramEffect()
        {
            if (_isLocked)
                return;

            _hologramEffect.Play();
        }

        public void ShowHologram()
        {
            if (_isLocked)
                return;

            _hologramEffect.Stop();
        }

        public void ShowHologram(object owner)
        {
            _lockHandler.Acquire(owner, () =>
            {
                _isLocked = true;
                _hologramEffect.Stop();
            });
        }

        public void PlayFadeHologramEffect(object owner)
        {
            _lockHandler.Release(owner, () =>
            {
                _hologramEffect.Play();
                _isLocked = false;
            });
        }

        protected override void UpdateFirstSegmentColorReduced()
        {
            base.UpdateFirstSegmentColorReduced();

            _segmentsOwn[FirstElement].color = _firstReducedColor;
        }

        protected override void UpdateFirstSegmentColorIncrease()
        {
            base.UpdateFirstSegmentColorIncrease();

            _segmentsOwn[FirstElement].color = _firstIncreaseColor;
        }

        private void UpdateGlowDarkWidth()
        {
            Vector2 sd = _darkGlowRectTransform.sizeDelta;
            sd.x += _stepWidth;
            _darkGlowRectTransform.sizeDelta = sd;
        }
    }
}