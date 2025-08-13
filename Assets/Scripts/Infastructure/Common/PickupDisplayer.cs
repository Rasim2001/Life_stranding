using Common;
using UnityEngine;

namespace Infastructure.Common
{
    public class PickupDisplayer : MonoBehaviour, IPickupDisplayer
    {
        [SerializeField] GameObject _pickupDisplayer;

        private Transform _pickUpTarget;
        private RotateToCamera _rotateToCamera;

        private void Start() =>
            _rotateToCamera = new RotateToCamera();

        public void Show(Transform pickupTarget)
        {
            _pickUpTarget = pickupTarget;
            transform.position = _pickUpTarget.position + Vector3.up;

            _pickupDisplayer.SetActive(true);
        }

        public void Hide()
        {
            _pickUpTarget = null;
            _pickupDisplayer.SetActive(false);
        }

        private void Update()
        {
            if (_pickUpTarget == null)
                return;

            _rotateToCamera.UpdateRotationPickUp(transform);
        }
    }
}