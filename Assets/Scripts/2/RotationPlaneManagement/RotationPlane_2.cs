using UnityEngine;

namespace _2.RotationPlaneManagement
{
    public class RotationPlane_2 : RotationPlaneBase
    {
        [SerializeField] private PlaneIndicator _planeIndicator;

        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _maxAngle = 45f;
        [SerializeField] private float _rotationSpeed;

        private Vector2 _mouseInput;
        private Vector2 _initialMousePosition;
        private bool _isMouseHeld;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _planeIndicator.Show();
                _isMouseHeld = true;
                _initialMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _planeIndicator.Hide();
                _isMouseHeld = false;
            }

            if (_isMouseHeld)
                HandleMousePosition();
        }

        private void FixedUpdate()
        {
            ApplyRotation();
        }

        private void HandleMousePosition()
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            _mouseInput.x = (mousePos.x - _initialMousePosition.x) / (screenSize.x / 2);
            _mouseInput.y = (mousePos.y - _initialMousePosition.y) / (screenSize.y / 2);

            _mouseInput = Vector2.ClampMagnitude(_mouseInput, 1f);

            _mouseInput.x = -_mouseInput.x;
            _mouseInput.y = -_mouseInput.y;

            _mouseInput *= _mouseSensitivity;
        }

        private void ApplyRotation()
        {
            float targetAngleX = Mathf.Clamp(-_mouseInput.y * _maxAngle, -_maxAngle, _maxAngle);
            float targetAngleZ = Mathf.Clamp(_mouseInput.x * _maxAngle, -_maxAngle, _maxAngle);

            Quaternion targetLocalRotation = Quaternion.Euler(targetAngleX, 0f, targetAngleZ);

            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocalRotation,
                Time.fixedDeltaTime * _rotationSpeed);
        }
    }
}