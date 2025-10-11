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

        private HologramEffect _hologramEffect;
        private Image[] _segmentsOwn;

        private void Awake()
        {
            _segmentsOwn = GetSegments();

            _hologramEffect = new HologramEffect(_segmentsOwn, GetContainers(), GetOtherObjects());
        }

        public override void AddNewSegment()
        {
            base.AddNewSegment();

            UpdateGlowDarkWidth();
            ShowHologram();
        }

        public void PlayFadeHologramEffect() =>
            _hologramEffect.Play();

        public void ShowHologram() =>
            _hologramEffect.Stop();

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