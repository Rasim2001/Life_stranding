using UnityEngine;

namespace PickupObjects.Rewind
{
    public struct PickupObjectSnapshot
    {
        public float Time;

        public RigidbodyConstraints Constraints;
        public bool UseGravity;

        public float AngularDamping;
        public float LinearDamping;

        public Vector3 WorldPosition;
        public Quaternion WorldRotation;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;

        public bool IsKinematic;
        public bool ColliderEnabled;
        public bool IsOnPlatform;
        public bool WasOnPlatform;
        public bool IsPuttingDown;

        public Transform Parent;
    }
}