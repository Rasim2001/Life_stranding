using UnityEngine;

namespace _2.RotationPlaneManagement
{
    public class RotationPlane_3 : RotationPlaneBase
    {
        [SerializeField] private PlaneIndicator _planeIndicator;

        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _maxAngle = 45f;
        [SerializeField] private float _rotationSpeed;


        private Vector2 _mouseInput;


        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                _planeIndicator.Show();
            else if (Input.GetMouseButtonUp(0))
                _planeIndicator.Hide();

            if (Input.GetMouseButton(0))
                HandleMousePosition();
        }

        private void FixedUpdate() =>
            ApplyRotation();

        private void HandleMousePosition()
        {
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 mousePos = Input.mousePosition;

            _mouseInput.x = (mousePos.x - screenCenter.x) / screenCenter.x;
            _mouseInput.y = (mousePos.y - screenCenter.y) / screenCenter.y;

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