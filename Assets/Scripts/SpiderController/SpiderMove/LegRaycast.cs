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

        public int RayDistance = 5;

        private Vector3 _startPosition;

        private void Awake() =>
            _startPosition = transform.localPosition;

        public void SetGroundState()
        {
            //transform.localPosition = _startPosition;
            RayDistance = 5;
        }

        public void SetAirbornState()
        {
            //transform.localPosition = Vector3.zero;
            RayDistance = 2;
        }

        private void Update()
        {
            Ray ray = new Ray(transform.position, -transform.up);

            Debug.DrawRay(ray.origin, ray.direction * RayDistance,
                Physics.Raycast(ray, out _hit, RayDistance, _layerMask) ? Color.green : Color.red);
        }
    }
}