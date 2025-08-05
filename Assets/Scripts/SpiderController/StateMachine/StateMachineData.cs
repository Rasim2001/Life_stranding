using UnityEngine;

namespace SpiderController.StateMachine
{
    public class StateMachineData
    {
        public Vector3 Input;
        public Vector3 Velocity;

        public float Speed;

        public float YVelocity;
        public float XVelocity;

        public float AirbornSpeed;

        public bool IsCenterMouseHolding;
        public float EnergyFillAmount = 1;
    }
}