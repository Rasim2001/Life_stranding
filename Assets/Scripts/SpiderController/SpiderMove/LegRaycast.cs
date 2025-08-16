using System;
using DG.Tweening;
using SpiderController.StateMachine;
using UnityEngine;
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
        public Vector3 FallingDownPosition => _airbornHit.point;

        private readonly float _positionSmoothSpeed = 20f;
        private readonly float _airbornRayDistance = 100;

        private RaycastHit _hit;
        private RaycastHit _airbornHit;

        private float _rayDistance = 5;


        private Vector3 _defaultRotation;
        private Tween _randomRotationTween;
        private Tween _defaultRotationTween;

        private Vector3 _smoothedPoint;

        private void Awake() =>
            _defaultRotation = transform.localEulerAngles;

        public void SetGroundState() =>
            _rayDistance = 5;

        public void SetAirbornState() =>
            _rayDistance = 2;

        public void RotateFallingLegs()
        {
            float randomAngleX = Random.Range(-50f, 50f);
            float randomAngleY = Random.Range(-50f, 50f);
            float randomAngleZ = Random.Range(-50f, 50f);

            Vector3 targetRandomRotation = new Vector3(randomAngleX, randomAngleY, randomAngleZ);

            _defaultRotationTween?.Kill();
            _randomRotationTween = transform.DOLocalRotate(targetRandomRotation, 0.5f);
        }

        public void SetDefaultRotationLegs()
        {
            _randomRotationTween?.Kill();
            _defaultRotationTween = transform.DOLocalRotate(_defaultRotation, 2);
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
                : _airbornHit.collider != null
                    ? _airbornHit.point
                    : origin + baseDirection * _rayDistance;

            _smoothedPoint = Vector3.Lerp(_smoothedPoint, targetPoint,
                1f - Mathf.Exp(-_positionSmoothSpeed * Time.deltaTime));

            if (hitFound && _hit.collider != null)
                Debug.DrawRay(mainRay.origin, mainRay.direction * _rayDistance, Color.green);
            else
                Debug.DrawRay(mainRay.origin, mainRay.direction * _rayDistance, Color.red);
        }

        private bool FindPlaceZ(Vector3 baseDirection, Vector3 origin)
        {
            bool hitFound = false;

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