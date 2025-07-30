using System;
using UnityEngine;

namespace SpiderController.SpiderMove
{
    public class LegTarget : MonoBehaviour
    {
        [SerializeField] private float _stepSpeed = 5f;
        [SerializeField] private AnimationCurve _stepCurve;

        public bool IsAirbornState;
        public Vector3 Position => _position;
        public bool IsMoving => _movement.IsMoving;

        private Movement _movement;
        private Vector3 _position;
        private float _stepSpeedDefault;


        private void Awake()
        {
            _movement = new Movement();
            _stepSpeedDefault = _stepSpeed;
        }

        private void Update()
        {
            if (_movement.IsMoving)
            {
                _movement.Progress = Mathf.Clamp01(_movement.Progress + Time.deltaTime * _stepSpeed);
                _position = _movement.Evaluate(transform.up, _stepCurve);

                if (_movement.Progress >= 1f)
                    _movement.IsMoving = false;
            }

            transform.position = IsAirbornState ? GetSmoothAirbornPosition() : _position;
        }


        public void SetAcceleration(float multiplayer) =>
            _stepSpeed *= multiplayer;

        public void SetDefaultSpeed() =>
            _stepSpeed = _stepSpeedDefault;

        public void MoveTo(Vector3 targetPosition)
        {
            if (_movement.IsMoving)
                _movement.ToPosition = targetPosition;
            else
            {
                _movement.Progress = 0;
                _movement.FromPosition = _position;
                _movement.ToPosition = targetPosition;
            }

            _movement.IsMoving = true;
        }

        private Vector3 GetSmoothAirbornPosition() =>
            Vector3.Lerp(transform.position, _movement.ToPosition,
                Time.deltaTime * 10);

        [Serializable]
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