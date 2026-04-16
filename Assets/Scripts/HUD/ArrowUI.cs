using TMPro;
using UnityEngine;

namespace HUD
{
    public class ArrowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _remainingDistance;

        public RectTransform ArrowCenter;

        private Transform _spiderTransform;
        private Transform _targetTransform;


        public void Initialize(Transform spiderTransform, Transform targetTransform)
        {
            _targetTransform = targetTransform;
            _spiderTransform = spiderTransform;
        }


        private void Update()
        {
            float distance =
                Mathf.Abs(Vector3.Distance(_spiderTransform.position, _targetTransform.position));

            _remainingDistance.text = $"{(int)distance}м";
        }
    }
}