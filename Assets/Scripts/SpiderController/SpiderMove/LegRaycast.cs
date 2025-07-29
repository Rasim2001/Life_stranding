using UnityEngine;

namespace SpiderController.SpiderMove
{
    public class LegRaycast : MonoBehaviour
    {
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private float _offsetDistance = 15f;
        [SerializeField] private int _offsetRayCount = 3;

        private RaycastHit _hit;
        public Vector3 Position => _hit.point;
        public Vector3 Normal => _hit.normal;
        public bool IsGrounded => _hit.collider != null;

        private float _rayDistance = 5;

        public void SetGroundState() =>
            _rayDistance = 5;

        public void SetAirbornState() =>
            _rayDistance = 2;

        private void Update()
        {
            Vector3 origin = transform.position;
            Vector3 baseDirection = -transform.up;

            Ray mainRay = new Ray(origin, baseDirection);
            bool hitFound = Physics.Raycast(mainRay, out _hit, _rayDistance, _layerMask);

            Debug.DrawRay(mainRay.origin, mainRay.direction * _rayDistance, Color.blue);

            if (!hitFound)
            {
                for (int x = 0; x <= _offsetRayCount - 1; x++)
                {
                    float angleOffset = _offsetDistance * x;

                    Vector3 rightDirection = Quaternion.AngleAxis(angleOffset, transform.right) * baseDirection;
                    Ray rightRay = new Ray(origin, rightDirection);

                    if (Physics.Raycast(rightRay, out _hit, _rayDistance, _layerMask))
                    {
                        hitFound = true;
                        Debug.DrawRay(rightRay.origin, rightRay.direction * _rayDistance, Color.green);
                        break;
                    }

                    Debug.DrawRay(rightRay.origin, rightRay.direction * _rayDistance, Color.yellow);

                    Vector3 leftDirection = Quaternion.AngleAxis(-angleOffset, transform.right) * baseDirection;
                    Ray leftRay = new Ray(origin, leftDirection);

                    if (Physics.Raycast(leftRay, out _hit, _rayDistance, _layerMask))
                    {
                        hitFound = true;
                        Debug.DrawRay(leftRay.origin, leftRay.direction * _rayDistance, Color.green);
                        break;
                    }

                    Debug.DrawRay(leftRay.origin, leftRay.direction * _rayDistance, Color.yellow);
                }

                if (!hitFound)
                {
                    for (int z = 0; z <= _offsetRayCount - 1; z++)
                    {
                        float angleOffset = _offsetDistance * z;

                        Vector3 rightDirection = Quaternion.AngleAxis(angleOffset, transform.forward) * baseDirection;
                        Ray rightRay = new Ray(origin, rightDirection);

                        if (Physics.Raycast(rightRay, out _hit, _rayDistance, _layerMask))
                        {
                            hitFound = true;
                            Debug.DrawRay(rightRay.origin, rightRay.direction * _rayDistance, Color.green);
                            break;
                        }

                        Debug.DrawRay(rightRay.origin, rightRay.direction * _rayDistance, Color.yellow);

                        Vector3 leftDirection = Quaternion.AngleAxis(-angleOffset, transform.forward) * baseDirection;
                        Ray leftRay = new Ray(origin, leftDirection);

                        if (Physics.Raycast(leftRay, out _hit, _rayDistance, _layerMask))
                        {
                            hitFound = true;
                            Debug.DrawRay(leftRay.origin, leftRay.direction * _rayDistance, Color.green);
                            break;
                        }

                        Debug.DrawRay(leftRay.origin, leftRay.direction * _rayDistance, Color.yellow);
                    }
                }
            }

            if (hitFound && _hit.collider != null)
                Debug.DrawRay(mainRay.origin, mainRay.direction * _rayDistance, Color.green);
            else
                Debug.DrawRay(mainRay.origin, mainRay.direction * _rayDistance, Color.red);
        }
    }
}