using System;
using _1;
using UnityEngine;

namespace _2
{
    [RequireComponent(typeof(Rigidbody))]
    public class Player_2 : MonoBehaviour
    {
        [SerializeField] private LayerMask _layerMask;

        [SerializeField] private LegData_2[] _legs;
        [SerializeField] private float _stepLength = 0.75f;
        [SerializeField] private float _speed = 2;
        [SerializeField] private float _lerpForwardSpeed = 2;
        [SerializeField] private float _distanceFromGround = 0.5f;
        [SerializeField] private float _lerpSpeedFromGround = 2;

        [Header("Jump Settings")]
        [SerializeField] private float _jumpForce = 8f;
        [SerializeField] private bool _isJumping;

        private Vector3 _inputVector;

        private Rigidbody _rigidbody;
        private Vector3 _averageNormal;
        private float _speedDefault;

        private RaycastHit _hit;


        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            _speedDefault = _speed;
        }

        [Obsolete("Obsolete")]
        private void Update()
        {
            if (HandleJump())
                return;

            InputHandle();
            HandleAcceleration();


            if (_isJumping && _rigidbody.velocity.y <= 0)
            {
                Ray ray = new Ray(transform.position, Vector3.down);
                Physics.Raycast(ray, out _hit, 0.5f, _layerMask);

                if (_hit.collider != null)
                {
                    Debug.Log("Jumping");

                    _isJumping = false;
                    _rigidbody.useGravity = false;

                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;

                    foreach (LegData_2 leg in _legs)
                        leg.Raycast.SetDefaultPosition();
                }

                return;
            }

            if (_isJumping)
                return;

            TryMoveLegs();
            //AdjustBodyHeight();
            //AdjustBodyOrientation();
        }

        private bool HandleJump()
        {
            bool jumpPressed = Input.GetKeyDown(KeyCode.Space) && !_isJumping;

            if (jumpPressed)
                Jump();

            return jumpPressed;
        }

        private void Jump()
        {
            _isJumping = true;
            _rigidbody.useGravity = true;

            foreach (LegData_2 leg in _legs)
                leg.Raycast.SetJumpPosition();

            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        }

        private void HandleAcceleration()
        {
            int mulyiplayer = 2;

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                _speed *= mulyiplayer;

                foreach (LegData_2 legData in _legs)
                    legData.Leg.SetAcceleration(mulyiplayer);
            }

            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                _speed = _speedDefault;

                foreach (LegData_2 legData in _legs)
                    legData.Leg.SetDefaultSpeed();
            }
        }

        private void FixedUpdate()
        {
            if (_isJumping)
                return;

            MoveBodySpider();
            RotateTowardsMoveDirection();
        }


        private void InputHandle() =>
            _inputVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));


        private void TryMoveLegs()
        {
            for (int index = 0; index < _legs.Length; index++)
            {
                ref LegData_2 legData = ref _legs[index];

                if (!CanMove(index))
                    continue;

                if (!legData.Leg.IsMoving &&
                    Vector3.Distance(legData.Leg.Position, legData.Raycast.Position) < _stepLength)
                    continue;

                if (legData.Raycast.IsGrounded)
                    legData.Leg.MoveTo(legData.Raycast.Position);
            }
        }

        private bool CanMove(int legIndex)
        {
            int legsCount = _legs.Length;
            LegData_2 n1 = _legs[(legIndex + legsCount - 1) % legsCount];
            LegData_2 n2 = _legs[(legIndex + 1) % legsCount];

            return !n1.Leg.IsMoving && !n2.Leg.IsMoving;
        }


        private void MoveBodySpider()
        {
            Vector3 localInput = new Vector3(_inputVector.x, 0f, _inputVector.z);
            Vector3 forwardMovement = transform.forward * (_inputVector.z * _speed * Time.fixedDeltaTime);
            Vector3 newPosition = _rigidbody.position + forwardMovement;

            if (localInput.sqrMagnitude < Mathf.Epsilon)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            else if (Mathf.Abs(localInput.z) > Mathf.Epsilon)
                _rigidbody.MovePosition(newPosition);
        }


        private void AdjustBodyHeight()
        {
            Vector3 avgLegPos = Vector3.zero;

            for (int i = 0; i < _legs.Length; i++)
                avgLegPos += _legs[i].Raycast.Position;

            avgLegPos /= _legs.Length;

            Vector3 localAvgLegPos = transform.InverseTransformPoint(avgLegPos);
            float targetY = localAvgLegPos.y + _distanceFromGround;

            Vector3 localPos = transform.InverseTransformPoint(_rigidbody.position);
            localPos.y = Mathf.Lerp(localPos.y, targetY, Time.deltaTime * _lerpSpeedFromGround);

            Vector3 worldPos = transform.TransformPoint(localPos);
            _rigidbody.MovePosition(worldPos);
        }

        private void AdjustBodyOrientation()
        {
            if (_inputVector.sqrMagnitude <= Mathf.Epsilon)
                return;

            Vector3[] legPositions = new Vector3[_legs.Length];
            for (int i = 0; i < _legs.Length; i++)
                legPositions[i] = _legs[i].Raycast.Position;

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

            _averageNormal = normalSum / count;
            _averageNormal.Normalize();

            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, _averageNormal) * _rigidbody.rotation;

            Vector3 euler = targetRotation.eulerAngles;
            euler.y = _rigidbody.rotation.eulerAngles.y;

            Quaternion noYawRotation = Quaternion.Euler(euler);

            Quaternion smoothedRotation = Quaternion.Slerp(_rigidbody.rotation, noYawRotation,
                Time.fixedDeltaTime * _lerpSpeedFromGround);

            _rigidbody.MoveRotation(smoothedRotation);
        }


        private void RotateTowardsMoveDirection()
        {
            Vector3 movementVector = new Vector3(_inputVector.x, 0f, _inputVector.z);

            if (Mathf.Abs(movementVector.x) > Mathf.Epsilon)
            {
                float rotationAmount = movementVector.x * _lerpForwardSpeed * Time.fixedDeltaTime;
                float totalRotation = _inputVector.z >= 0 ? rotationAmount : -rotationAmount;

                Quaternion deltaRotation = Quaternion.Euler(0, totalRotation, 0);
                Quaternion newRotation = _rigidbody.rotation * deltaRotation;

                _rigidbody.MoveRotation(newRotation);
            }
        }


        [Serializable]
        private struct LegData_2
        {
            public LegTarget_2 Leg;
            public LegRaycast Raycast;
        }
    }
}