using UnityEngine;

namespace _2
{
    public class FlowerFollower : MonoBehaviour
    {
        private Transform _target;

        public void SetTarget(Transform spiderTransform) =>
            _target = spiderTransform;

        private void Update()
        {
            if (_target == null)
                return;

            transform.position = _target.transform.position;
            transform.rotation = _target.transform.rotation;
        }
    }
}