using System;
using UnityEngine;

namespace SpiderController.StateMachine
{
    public class StateMachineData
    {
        public event Action OnFallingDownStateChanged;

        public bool IsFallingDownWithoutEnergyState
        {
            get => _IsFallingDownWithoutEnergyState;
            set
            {
                if (_IsFallingDownWithoutEnergyState != value)
                {
                    OnFallingDownStateChanged?.Invoke();

                    _IsFallingDownWithoutEnergyState = value;
                }
            }
        }

        public Vector3 Input;
        public Vector3 Velocity;

        public float DistanceFromGround = 0.5f;
        public float Speed;
        public float YVelocity;
        public float XVelocity;
        public float AirbornSpeed;
        public float EnergyFillAmount = 1;


        public bool IsMouseHolding;
        public bool IsStandingUpAfterFalling;
        private bool _IsFallingDownWithoutEnergyState;
    }
}