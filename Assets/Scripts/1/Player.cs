using System;
using UnityEngine;

namespace _1
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private LegData[] _legs;
        [SerializeField] private float _stepLength = 0.75f;
        [SerializeField] private float _speed;
        [SerializeField] private float _bodyHeightOffset;
        [SerializeField] private float _lerpHeightSpeed;
        [SerializeField] private float _lerpAngleHeightSpeed;
        [SerializeField] private float _lerpForwardSpeed;

        private bool _moveEvenLegs = true;

        private Rigidbody _rigidbody;

        private Vector3 _movement;
        private Camera _camera;

        private float _slopeAngle;


        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            _camera = Camera.main;
        }

        private void Update()
        {
            InputHandle();

            if (AllCurrentLegsFinished())
                UpdateMoveLegs();

            TryMoveLegs();
        }

        private void FixedUpdate()
        {
            Move();

            RotateTowardsMoveDirection();

            AdjustBodyHeight();

            AdjustBodyOrientation();
        }


        private void RotateTowardsMoveDirection()
        {
            Vector3 movementVector = new Vector3(_movement.x, 0f, _movement.z);

            if (movementVector.sqrMagnitude > Mathf.Epsilon)
            {
                movementVector = _camera.transform.TransformDirection(movementVector);
                movementVector.y = 0;
                movementVector.Normalize();

                Vector3 currentForward = _rigidbody.rotation * Vector3.forward;

                Vector3 newForward = Vector3.Slerp(
                    currentForward,
                    movementVector,
                    Time.fixedDeltaTime * _lerpForwardSpeed
                );

                Quaternion newRotation = Quaternion.LookRotation(newForward);
                _rigidbody.MoveRotation(newRotation);
            }
        }

        private void AdjustBodyHeight()
        {
            bool isFlatGround = true;

            Vector3 avgLegPos = Vector3.zero;

            for (int i = 0; i < _legs.Length; i++)
            {
                avgLegPos += _legs[i].Leg.Position;

                if (_legs[i].Raycast.Normal != Vector3.up && isFlatGround)
                    isFlatGround = false;
            }

            avgLegPos /= _legs.Length;

            float targetY = avgLegPos.y + _bodyHeightOffset;

            if (Mathf.Abs(_rigidbody.position.y - targetY) < 0.1f && isFlatGround)
                return;

            Vector3 pos = _rigidbody.position;
            pos.y = Mathf.Lerp(_rigidbody.position.y, targetY, Time.fixedDeltaTime * _lerpHeightSpeed);

            _rigidbody.MovePosition(pos);
        }

        private void AdjustBodyOrientation()
        {
            if (_movement.sqrMagnitude <= Mathf.Epsilon)
                return;

            Vector3[] legPositions = new Vector3[_legs.Length];
            for (int i = 0; i < _legs.Length; i++)
                legPositions[i] = _legs[i].Leg.Position;

            Vector3 normalSum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < _legs.Length; i++)
            {
                int i1 = (i + 1) % _legs.Length;
                int i2 = (i + 2) % _legs.Length;
                int i3 = (i + 3) % _legs.Length;

                Vector3 v1 = legPositions[i2] - legPositions[i1];
                Vector3 v2 = legPositions[i3] - legPositions[i1];
                Vector3 normal = Vector3.Cross(v1, v2).normalized;

                if (normal.y < 0)
                    normal = -normal;

                normalSum += normal;
                count++;
            }

            Vector3 averageNormal = normalSum / count;
            averageNormal.Normalize();

            _slopeAngle = Vector3.Angle(Vector3.up, averageNormal);

            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, averageNormal) * _rigidbody.rotation;
            Quaternion smoothedRotation = Quaternion.Slerp(_rigidbody.rotation, targetRotation,
                Time.fixedDeltaTime * _lerpAngleHeightSpeed);

            _rigidbody.MoveRotation(smoothedRotation);
        }

        private void InputHandle()
        {
            Vector3 inputVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            float speedModifier = Mathf.Clamp01(1.0f - _slopeAngle / 90.0f * 0.75f);

            _movement = inputVector.normalized * (_speed * speedModifier);
        }

        private void Move() =>
            _rigidbody.MovePosition(transform.position + _movement * Time.fixedDeltaTime);


        private void UpdateMoveLegs() =>
            _moveEvenLegs = !_moveEvenLegs;

        private void TryMoveLegs()
        {
            for (int index = 0; index < _legs.Length; index++)
            {
                ref LegData legData = ref _legs[index];

                if (!legData.Leg.IsMoving &&
                    Vector3.Distance(legData.Leg.Position, legData.Raycast.Position) > _stepLength)
                    legData.Leg.MoveTo(legData.Raycast.Position);

                /*if ((_moveEvenLegs && index % 2 == 0) || (!_moveEvenLegs && index % 2 != 0))
                {
                }*/
            }
        }


        private bool AllCurrentLegsFinished()
        {
            for (int i = 0; i < _legs.Length; i++)
            {
                if ((_moveEvenLegs && i % 2 == 0) || (!_moveEvenLegs && i % 2 != 0))
                {
                    if (_legs[i].Leg.IsMoving)
                        return false;
                }
            }

            return true;
        }

        [Serializable]
        private struct LegData
        {
            public LegTarget Leg;
            public LegRaycast Raycast;
        }
    }
}