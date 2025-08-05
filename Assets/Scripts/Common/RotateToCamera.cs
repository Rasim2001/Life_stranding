using UnityEngine;

namespace Common
{
    public class RotateToCamera
    {
        private readonly Transform _cameraTransform;

        public RotateToCamera(Camera camera) =>
            _cameraTransform = camera.transform;

        public void UpdateRotation(Transform target)
        {
            Vector3 lookDir = target.position - _cameraTransform.position;
            lookDir.y = 0f;

            if (lookDir != Vector3.zero)
                target.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}