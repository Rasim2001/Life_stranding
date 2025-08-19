
using UnityEngine;

namespace SpiderController
{
    public class FlowerChecker : MonoBehaviour
    {
        [SerializeField] private LayerMask _flowerLayer;
        [SerializeField] private float _radius;

        public bool IsTouching;

        private void Update() =>
            IsTouching = Physics.CheckSphere(transform.position, _radius, _flowerLayer);
    }
}