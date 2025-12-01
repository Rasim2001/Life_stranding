using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SpiderController.UI
{
    public class BarBaseUI : MonoBehaviour
    {
        [SerializeField] private Image[] _segments;
        [SerializeField] private Image[] _containers;
        [SerializeField] private Image[] _otherObjects;

        private float PerSegment => 1f / _segmentContainerCount;

        private int _segmentContainerCount;
        private bool _isReduced;

        protected virtual void Start() =>
            _segmentContainerCount = _containers.Count(x => x.gameObject.activeSelf);

        public virtual void AddNewSegment()
        {
            if (_segmentContainerCount == _containers.Length)
                return;

            _containers[_segmentContainerCount].gameObject.SetActive(true);

            _segmentContainerCount++;
        }

        public void SetValue(float normalizedValue)
        {
            for (int i = 0; i < _segmentContainerCount; i++)
            {
                float segmentFill = Mathf.Clamp01((normalizedValue - i * 1f / _segmentContainerCount) / PerSegment);
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