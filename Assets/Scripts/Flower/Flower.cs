using UnityEngine;

namespace SpiderController
{
    [RequireComponent(typeof(Rigidbody))]
    public class Flower : MonoBehaviour
    {
        [SerializeField] private Transform _platform;
        [SerializeField] private Bounds _platformBounds;
        [SerializeField] private float _speed = 1;
        public Rigidbody Rigidbody => _rigidbody;
        public bool IsOnPlatform => _isOnPlatform;

        public bool IsFreezingOnPlatform;

        private bool _isOnPlatform = true;
        private Rigidbody _rigidbody;
        private Vector3 _startPosition;
        private Quaternion _startRotation; // Добавляем сохранение начального поворота

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _startPosition = transform.localPosition;
            _startRotation = transform.localRotation;
        }

        private void Update()
        {
            if (!_isOnPlatform || IsFreezingOnPlatform)
                return;

            Vector3 localPos = transform.localPosition;
            _isOnPlatform = _platformBounds.Contains(localPos);

            if (_isOnPlatform)
                SimulateRotation();
            else
                SimulatePhysics();
        }

        public void ResetSimulate()
        {
            transform.SetParent(_platform);
            transform.localPosition = _startPosition;
            transform.localRotation = _startRotation;

            _rigidbody.isKinematic = true;
            _isOnPlatform = true;
        }

        private void SimulateRotation()
        {
            Vector3 platformRotation = _platform.eulerAngles;

            float angleX = Mathf.Deg2Rad * platformRotation.x;
            float angleZ = Mathf.Deg2Rad * platformRotation.z;

            Vector3 gravityForce = new Vector3(
                -Mathf.Sin(angleZ),
                0f,
                -Mathf.Sin(angleX)
            );

            Vector3 movementVector = gravityForce * (Time.deltaTime * _speed);
            movementVector = _startRotation * movementVector;

            transform.Translate(movementVector, Space.Self);
        }

        private void SimulatePhysics()
        {
            transform.SetParent(null);

            _isOnPlatform = false;
            _rigidbody.isKinematic = false;
        }
    }
}