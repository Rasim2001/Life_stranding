using UnityEngine;

namespace _2
{
    public class LegRaycast : MonoBehaviour
    {
        [SerializeField] private LayerMask _layerMask;

        private RaycastHit _hit;
        public Vector3 Position => _hit.point;
        public Vector3 Normal => _hit.normal;

        public bool IsGrounded => _hit.collider != null;

        private int rayDistance = 5;

        private Vector3 _startPosition;

        private void Awake() =>
            _startPosition = transform.localPosition;

        public void SetJumpPosition() =>
            transform.localPosition = Vector3.zero;

        public void SetDefaultPosition() =>
            transform.localPosition = _startPosition;

        private void Update()
        {
            Ray ray = new Ray(transform.position, -transform.up);

            Debug.DrawRay(ray.origin, ray.direction * rayDistance,
                Physics.Raycast(ray, out _hit, rayDistance, _layerMask) ? Color.green : Color.red);
        }
    }
}