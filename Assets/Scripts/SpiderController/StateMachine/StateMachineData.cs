using System;
using UnityEngine;

namespace SpiderController.StateMachine
{
    [Serializable]
    public class StateMachineData
    {
        public event Action AimingStateChanged;
        public event Action<bool> OnFallingDownStateChanged;
        public event Action OnTotalWeightChanged;

        public bool IsFallingDownWithoutEnergyState
        {
            get => _IsFallingDownWithoutEnergyState;
            set
            {
                if (_IsFallingDownWithoutEnergyState != value)
                {
                    OnFallingDownStateChanged?.Invoke(value);

                    _IsFallingDownWithoutEnergyState = value;
                }
            }
        }

        public Vector3 Input;
        public Vector3 Velocity;
        public Vector3 ExplosionVector;
        public Vector3 ExplosionAngularVector;
        public Vector3 LastValidGroundPosition;
        public Quaternion LastValidGroundRotation;
        public bool IsInAimingState
        {
            get => _isInAimingState;
            set
            {
                bool newValue = value;

                if (newValue != _isInAimingState)
                {
                    _isInAimingState = value;

                    AimingStateChanged?.Invoke();
                }
            }
        }

        public bool IsInGravityGunState;

        public float RotationAmount;

        public float DistanceFromGround = 0.5f;
        public float Speed
        {
            get => _speed;

            set
            {
                float newValue = value;

                float actualSpeed = newValue / (1 + _slowdownFactor * TotalWeight);

                _speed = actualSpeed;
            }
        }

        public float YVelocity;
        public float XVelocity;
        public float GlobalY;
        public float AirbornSpeed;
        public float CurrentEnergyFillAmount = 1;
        public float EnergyFillAmount;
        public float TotalWeight
        {
            get => _totalWeight;
            set
            {
                _totalWeight = value;
                OnTotalWeightChanged?.Invoke();
            }
        }

        public bool IsMouseHolding;
        public bool IsStandingUpAfterFalling;

        public float TerrainTimer;
        public float TerrainTimerDefault;


        private readonly float _slowdownFactor = 0.25f;
        private float _speed;
        private bool _IsFallingDownWithoutEnergyState;
        private float _totalWeight;
        private bool _isInAimingState;

        public void Clear()
        {
            Input = Vector3.zero;
            Velocity = Vector3.zero;
            ExplosionAngularVector = Vector3.zero;

            TotalWeight = 0;
        }
    }
}