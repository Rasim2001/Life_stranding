using UnityEngine;

namespace SpiderController.StateMachine.States.Rewind
{
    public struct SpiderSnapshot
    {
        public float Time;
        public float CurrentEnergy;
        public float CurrentHp;

        public int PlatformIndex;

        public Vector3 Position;
        public Vector3[] LegPositions;

        public Quaternion Rotation;
        public Quaternion LegsRootRotation;
        public Quaternion HeadRootRotation;
        public Quaternion RaycastRigRotation;
        public Quaternion PlaneRotation;
    }
}