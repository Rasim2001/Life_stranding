using UnityEngine;

namespace SpiderController.SpiderMove
{
    public class LegRaycast : MonoBehaviour
    {
        [SerializeField] private LayerMask _layerMask;

        private RaycastHit _hit;
        public Vector3 Position => _hit.point;
        public Vector3 Normal => _hit.normal;
        public bool IsGrounded => _hit.collider != null;

        private float _rayDistance = 5;

        public void SetGroundState() =>
            _rayDistance = 15;

        public void SetAirbornState() =>
            _rayDistance = 2;

        private void Update()
        {
            Ray ray = new Ray(transform.position, -transform.up);

            Debug.DrawRay(ray.origin, ray.direction * _rayDistance,
                Physics.Raycast(ray, out _hit, _rayDistance, _layerMask) ? Color.green : Color.red);
        }
    }
}