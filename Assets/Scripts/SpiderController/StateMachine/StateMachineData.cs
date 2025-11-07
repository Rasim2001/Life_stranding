using System;
using UnityEngine;

namespace SpiderController.StateMachine
{
    public class StateMachineData
    {
        public event Action<bool> OnFallingDownStateChanged;
        public Action<float> OnShakeHappened;

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

        public float RotationAmount;

        public float DistanceFromGround = 0.5f;
        public float Speed;
        public float YVelocity;
        public float XVelocity;
        public float GlobalY;
        public float AirbornSpeed;
        public float CurrentEnergyFillAmount = 1;
        public float EnergyFillAmount;

        public bool IsMouseHolding;
        public bool IsStandingUpAfterFalling;

        public float TerrainTimer;
        public float TerrainTimerDefault;

        public void Clear()
        {
            Input = Vector3.zero;
            Velocity = Vector3.zero;
            ExplosionAngularVector = Vector3.zero;
        }

        private bool _IsFallingDownWithoutEnergyState;
    }
}