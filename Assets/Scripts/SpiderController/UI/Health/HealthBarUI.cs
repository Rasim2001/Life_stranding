using System;
using UnityEngine;
using UnityEngine.UI;

namespace SpiderController.UI.Health
{
    public class HealthBarUI : BarBaseUI
    {
        [SerializeField] Color _firstIncreaseColor;
        [SerializeField] Color _firstReducedColor;

        private HologramEffect _hologramEffect;

        private Image[] _segmentsOwn;

        protected override void Awake()
        {
            base.Awake();

            _segmentsOwn = GetSegments();

            _hologramEffect = new HologramEffect(_segmentsOwn, GetContainers(), GetOtherObjects());
        }

        private void OnDestroy()
        {
            _hologramEffect.Clear();
        }

        public void PlayFadeHologramEffect()
        {
            _hologramEffect.Play();
        }

        public void ShowHologram()
        {
            _hologramEffect.Stop();
        }

        protected override void UpdateFirstSegmentColorReduced()
        {
            base.UpdateFirstSegmentColorReduced();

            _segmentsOwn[0].color = _firstReducedColor;
        }

        protected override void UpdateFirstSegmentColorIncrease()
        {
            base.UpdateFirstSegmentColorIncrease();

            _segmentsOwn[0].color = _firstIncreaseColor;
        }
    }
}