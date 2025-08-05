using UnityEngine;

namespace Infastructure.Common
{
    public class PickupDisplayer : MonoBehaviour, IPickupDisplayer
    {
        [SerializeField] GameObject _pickupDisplayer;
        public Transform SpiderTransform { get; set; }

        private Transform _pickUpTarget;
        private Transform _cameraTransform;

        public void Show(Transform pickupTarget)
        {
            if (_cameraTransform == null)
                _cameraTransform = Camera.main.transform;

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

            Vector3 lookDir = transform.position - _cameraTransform.position;
            lookDir.y = 0f;

            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}