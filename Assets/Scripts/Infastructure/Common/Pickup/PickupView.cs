using Common;
using UnityEngine;

namespace Infastructure.Common.Pickup
{
    public class PickupView : MonoBehaviour
    {
        private RotateToCamera _rotateToCamera;

        private void Start() =>
            _rotateToCamera = new RotateToCamera();

        private void Update() =>
            _rotateToCamera.UpdateRotationPickUp(transform);
    }
}