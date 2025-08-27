using UnityEngine;
using UnityEngine.UI;

namespace SpiderController.UI
{
    public class EnergyBarUI : MonoBehaviour
    {
        [SerializeField] private Image[] _segments;
        private int SegmentCount => _segments.Length;
        private float PerSegment => 1f / SegmentCount;

        public void SetEnergyValue(float energyValue)
        {
            for (int i = 0; i < SegmentCount; i++)
            {
                float segmentFill = Mathf.Clamp01((energyValue - i * PerSegment) / PerSegment);
                _segments[i].fillAmount = segmentFill;
            }
        }   
    }
}