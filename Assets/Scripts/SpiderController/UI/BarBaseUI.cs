using UnityEngine;
using UnityEngine.UI;

namespace SpiderController.UI
{
    public class BarBaseUI : MonoBehaviour
    {
        [SerializeField] private Image[] _segments;
        [SerializeField] private Image[] _containers;
        private int SegmentCount => _segments.Length;
        private float PerSegment => 1f / SegmentCount;

        public void SetValue(float normalizedValue)
        {
            for (int i = 0; i < _segments.Length; i++)
            {
                float segmentFill = Mathf.Clamp01((normalizedValue - i * 1f / SegmentCount) / PerSegment);
                _segments[i].fillAmount = segmentFill;
            }
        }

        protected Image[] GetSegments() => _segments;
        protected Image[] GetContainers() => _containers;
    }
}