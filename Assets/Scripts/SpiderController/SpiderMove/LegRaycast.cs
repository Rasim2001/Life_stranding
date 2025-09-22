using DG.Tweening;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace SpiderController.SpiderMove
{
    public class LegRaycast : MonoBehaviour
    {
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private float _offsetDistance = 15f;
        [SerializeField] private int _offsetRayCount = 5;

        public Vector3 Position => _smoothedPoint;
        public bool IsGrounded => _hit.collider != null;
        public Vector3 AirbornPosition => _airbornHit.point;

        private readonly float _positionSmoothSpeed = 20f;
        private readonly float _airbornRayDistance = Mathf.Infinity;

        private RaycastHit _hit;
        private RaycastHit _airbornHit;

        private float _rayDistance;

        private Tween _randomRotationTween;
        private Tween _defaultRotationTween;

        private Vector3 _smoothedPoint;
        private Vector3 _defaultPosition;
        private IStaticDataService _staticDataService;

        [Inject]
        public void Construct(IStaticDataService staticDataService) =>
            _staticDataService = staticDataService;

        private void Awake()
        {
            _rayDistance = _staticDataService.SpiderStaticData.GroundStateRayDistance;
            _defaultPosition = transform.localPosition;
        }

        public void SetGroundState()
        {
            _rayDistance = _staticDataService.SpiderStaticData.GroundStateRayDistance;

            ReturnBodyToDefault();
        }

        public void SetAirbornState()
        {
            _rayDistance = _staticDataService.SpiderStaticData.AirbornStateRayDistance;

            GroupBody();
        }

        private void Update()
        {
            Vector3 origin = transform.position;
            Vector3 baseDirection = -transform.up;

            Ray mainRay = new Ray(origin, baseDirection);
            bool hitFound = Physics.Raycast(mainRay, out _hit, _rayDistance, _layerMask);

            Physics.Raycast(mainRay, out _airbornHit, _airbornRayDistance, _layerMask);
            Debug.DrawRay(mainRay.origin, mainRay.direction * _airbornRayDistance, Color.magenta);

            Debug.DrawRay(mainRay.origin, mainRay.direction * _rayDistance, Color.blue);

            if (!hitFound)
            {
                hitFound = FindLegPlaceX(baseDirection, origin);

                if (!hitFound)
                    hitFound = FindPlaceZ(baseDirection, origin);

                if (!hitFound)
                {
                    Ray downRay = new Ray(origin, Vector3.down);

                    if (Physics.Raycast(downRay, out _hit, _rayDistance, _layerMask))
                    {
                        hitFound = true;
                        Debug.DrawRay(downRay.origin, downRay.direction * _rayDistance, Color.blue);
                    }
                }
            }


            Vector3 targetPoint = IsGrounded
                ? _hit.point
                : origin + baseDirection * _rayDistance;

            _smoothedPoint = Vector3.Lerp(_smoothedPoint, targetPoint,
                1f - Mathf.Exp(-_positionSmoothSpeed * Time.deltaTime));

            if (hitFound && _hit.collider != null)
                Debug.DrawRay(mainRay.origin, mainRay.direction * _rayDistance, Color.green);
            else
                Debug.DrawRay(mainRay.origin, mainRay.direction * _rayDistance, Color.red);
        }

        private void GroupBody()
        {
            float sign = transform.localPosition.x > 0 ? 0.6f : -0.6f;
            transform.localPosition = new Vector3(sign, transform.localPosition.y, transform.localPosition.z);
        }

        private void ReturnBodyToDefault() =>
            transform.localPosition = _defaultPosition;

        private bool FindPlaceZ(Vector3 baseDirection, Vector3 origin)
        {
            bool hitFound = false;

            for (int z = 0; z <= _offsetRayCount - 1; z++)
            {
                float angleOffset = 10 * z;

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

            return hitFound;
        }

        private bool FindLegPlaceX(Vector3 baseDirection, Vector3 origin)
        {
            bool hitFound = false;

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

            return hitFound;
        }
    }
}