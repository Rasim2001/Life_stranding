using DG.Tweening;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;

namespace SpiderController.SpiderMove
{
    public class LegRaycast : MonoBehaviour
    {
        private const string NotMoveableLayer = "NotMoveable";

        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private float _offsetAngle = 5f;
        [SerializeField] private int _offsetRayCount = 15;

        public Vector3 Position => _smoothedPoint;
        public Vector3 GroundNormal => IsGrounded ? _hit.normal : transform.up;
        public bool IsGrounded => _hit.collider != null;
        public Vector3 AirbornPosition => _airbornHit.point;
        public bool IsNotMoveableLayer => IsGrounded && _hit.collider.gameObject.layer == _notMoveableLayer;

        private readonly float _positionSmoothSpeed = 20f;
        private readonly float _airbornRayDistance = Mathf.Infinity;

        private RaycastHit _hit;
        private RaycastHit _airbornHit;

        private float _rayDistance;
        private int _notMoveableLayer;

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
            _smoothedPoint = transform.position;

            _notMoveableLayer = LayerMask.NameToLayer(NotMoveableLayer);
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

            if (!hitFound)
                hitFound = FindLegPlaceX(baseDirection, origin) || FindPlaceZ(baseDirection, origin);

            Vector3 targetPoint = hitFound ? _hit.point : origin + baseDirection * _rayDistance;

            _smoothedPoint = Vector3.Lerp(_smoothedPoint, targetPoint,
                1f - Mathf.Exp(-_positionSmoothSpeed * Time.deltaTime));
        }


        public void ForceImmediateUpdate()
        {
            Vector3 origin = transform.position;
            Vector3 baseDirection = -transform.up;

            if (Physics.Raycast(origin, baseDirection, out _hit, _rayDistance, _layerMask))
                _smoothedPoint = _hit.point;
            else
                _smoothedPoint = origin + baseDirection * _rayDistance;
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
            for (int z = 1; z <= _offsetRayCount; z++)
            {
                float angle = _offsetAngle * z;

                if (TryOffsetRay(baseDirection, origin, transform.forward, angle))
                    return true;
                if (TryOffsetRay(baseDirection, origin, transform.forward, -angle))
                    return true;
            }

            return false;
        }

        private bool FindLegPlaceX(Vector3 baseDirection, Vector3 origin)
        {
            for (int x = 1; x <= _offsetRayCount; x++)
            {
                float angle = _offsetAngle * x;

                if (TryOffsetRay(baseDirection, origin, transform.right, angle))
                    return true;

                if (TryOffsetRay(baseDirection, origin, transform.right, -angle))
                    return true;
            }

            return false;
        }

        private bool TryOffsetRay(Vector3 baseDir, Vector3 origin, Vector3 axis, float angle)
        {
            Vector3 dir = Quaternion.AngleAxis(angle, axis) * baseDir;
            if (Physics.Raycast(origin, dir, out _hit, _rayDistance, _layerMask))
            {
                Debug.DrawRay(origin, dir * _rayDistance, Color.green);
                return true;
            }

            Debug.DrawRay(origin, dir * _rayDistance, Color.yellow);
            return false;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_smoothedPoint, 0.05f);

            if (IsGrounded)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_hit.point, 0.08f);
                Gizmos.DrawLine(transform.position, _hit.point);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, transform.position - transform.up * _rayDistance);
            }
        }
    }
}