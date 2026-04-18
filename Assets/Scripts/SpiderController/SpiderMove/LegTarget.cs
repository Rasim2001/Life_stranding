using System;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;

namespace SpiderController.SpiderMove
{
    public class LegTarget : MonoBehaviour
    {
        [SerializeField] private float _stepSpeed = 5f;
        [SerializeField] private AnimationCurve _stepCurve;
        [SerializeField] private Transform _crossLegTransform;

        public bool IsCrossingLeg;

        public bool IsAirbornState
        {
            get => _isAirbornState;
            set
            {
                if (_isAirbornState && !value)
                {
                    _position = transform.position;

                    _movement.Progress = 0;
                    _movement.FromPosition = _position;
                }


                _isAirbornState = value;
            }
        }
        public Vector3 Position => _position;
        public bool IsMoving => _movement.IsMoving;

        private Movement _movement;
        private Vector3 _position;
        private float _stepSpeedDefault;
        private bool _isAirbornState;
        private IStaticDataService _staticDataService;


        private void Awake()
        {
            _movement = new Movement();
            _stepSpeedDefault = _stepSpeed;
        }

        [Inject]
        public void Construct(IStaticDataService staticDataService) =>
            _staticDataService = staticDataService;


        private void Update()
        {
            if (IsCrossingLeg)
            {
                transform.position = Vector3.Lerp(transform.position, _crossLegTransform.position,
                    _staticDataService.SpiderStaticData.CrossLerpSpeed * Time.deltaTime);

                return;
            }


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
        
        public void SetPositionImmediate(Vector3 position)
        {
            _movement.IsMoving = false;
            
            _position = position;
            transform.position = position;
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