using UnityEngine;

namespace _1
{
    [RequireComponent(typeof(Rigidbody))]
    public class LegTarget : MonoBehaviour
    {
        [SerializeField] private float _stepSpeed = 5f;
        [SerializeField] private AnimationCurve _stepCurve;
        [SerializeField] private float _positionOffsetY;

        public Vector3 Position => _position;
        public bool IsMoving => _movement.IsMoving;

        private Movement _movement;

        [SerializeField] private Vector3 _position;

        private Rigidbody _rigidbody;

        private void Awake() =>
            _movement = new Movement();

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _position = _rigidbody.position;

            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
        }

        private void FixedUpdate()
        {
            if (!_movement.IsMoving)
                return;

            _movement.Progress = Mathf.Clamp01(_movement.Progress + Time.fixedDeltaTime * _stepSpeed);
            _position = _movement.Evaluate(Vector3.up, _stepCurve);

            Vector3 targetPosition = _position + new Vector3(0, _positionOffsetY, 0);
            _rigidbody.MovePosition(targetPosition);

            if (_movement.Progress >= 1f)
                _movement.IsMoving = false;
        }


        public void MoveTo(Vector3 targetPosition)
        {
            if (_movement.IsMoving)
                _movement.ToPosition = targetPosition;
            else
            {
                _movement.Progress = 0;
                _movement.FromPosition = _position;
                _movement.ToPosition = targetPosition;
                _movement.IsMoving = true;
            }
        }

        private class Movement
        {
            public float Progress;
            public Vector3 FromPosition;
            public Vector3 ToPosition;

            public bool IsMoving;

            public Vector3 Evaluate(Vector3 up, AnimationCurve stepCurve) =>
                Vector3.Lerp(FromPosition, ToPosition, Progress) + up * stepCurve.Evaluate(Progress);
        }
    }
}