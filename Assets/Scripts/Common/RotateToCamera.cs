using UnityEngine;

namespace Common
{
    public class RotateToCamera
    {
        private readonly Transform _cameraTransform = Camera.main.transform;

        public void UpdateRotation(Transform target) =>
            target.rotation = _cameraTransform.rotation;

        public void UpdateRotationPickUp(Transform target)
        {
            Vector3 lookDir = target.position - _cameraTransform.position;
            lookDir.y = 0;

            if (lookDir != Vector3.zero)
                target.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}