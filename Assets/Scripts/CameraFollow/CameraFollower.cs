using UnityEngine;

namespace CameraFollow
{
    public class CameraFollower : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothTime = 0.3f;
        [SerializeField] private float _rotationSpeed = 5f;
        [SerializeField] private float _mouseSpeed = 5f;
        [SerializeField] private float _scrollSensitivity = 5f;

        private Vector3 _velocity;

        private bool _isMouseRotating;
        private float _currentYRotation;
        private float _mouseSensitivity;

        private Vector3 _offsetMovePosition;

        public void SetTarget(Transform spiderTransform) =>
            _target = spiderTransform;

        private void LateUpdate()
        {
            if (_target == null)
                return;

            HandleMouse();
            HandleScrollWheel();
        }

        private void FixedUpdate()
        {
            if (_target == null)
                return;

            MoveToTarget();

            if (!_isMouseRotating)
                RotateToTarget();
        }

        private void HandleScrollWheel()
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");

            if (scrollInput != 0f)
            {
                _mouseSensitivity -= scrollInput * _scrollSensitivity;
                _mouseSensitivity = Mathf.Clamp(_mouseSensitivity, 0, 5f);
            }

            _offsetMovePosition = new Vector3(0, _mouseSensitivity, 0);
        }

        private void HandleMouse()
        {
            if (Input.GetMouseButtonDown(1))
                _isMouseRotating = true;

            if (Input.GetMouseButtonUp(1))
                _isMouseRotating = false;

            if (_isMouseRotating)
            {
                float mouseX = Input.GetAxis("Mouse X");
                _currentYRotation += mouseX * _mouseSpeed * Time.deltaTime;

                transform.rotation = Quaternion.Euler(0, _currentYRotation, 0);
            }
        }

        private void MoveToTarget()
        {
            Vector3 targetPosition = _target.position + _offsetMovePosition;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _velocity,
                _smoothTime
            );
        }

        private void RotateToTarget()
        {
            _currentYRotation = transform.eulerAngles.y;

            Quaternion targetRotation = Quaternion.Euler(0, _target.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                Time.fixedDeltaTime * _rotationSpeed
            );
        }
    }
}