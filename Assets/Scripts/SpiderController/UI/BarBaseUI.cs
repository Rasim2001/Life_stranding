using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace SpiderController.UI
{
    public class BarBaseUI : MonoBehaviour
    {
        [SerializeField] private Image[] _segments;
        [SerializeField] private Image[] _containers;
        [SerializeField] private Image[] _otherObjects;

        private int SegmentCount => _segments.Length;
        private float PerSegment => 1f / SegmentCount;

        private bool _isReduced;

        public void SetValue(float normalizedValue)
        {
            for (int i = 0; i < _segments.Length; i++)
            {
                float segmentFill = Mathf.Clamp01((normalizedValue - i * 1f / SegmentCount) / PerSegment);
                _segments[i].fillAmount = segmentFill;

                if (i == 0 && segmentFill < 0.95f && !_isReduced)
                    UpdateFirstSegmentColorReduced();
                else if (i == 0 && segmentFill > 0.95f && _isReduced)
                    UpdateFirstSegmentColorIncrease();
            }
        }

        protected virtual void UpdateFirstSegmentColorReduced() =>
            _isReduced = true;

        protected virtual void UpdateFirstSegmentColorIncrease() =>
            _isReduced = false;

        protected Image[] GetSegments() => _segments;
        protected Image[] GetContainers() => _containers;
        protected Image[] GetOtherObjects() => _otherObjects;
    }
}