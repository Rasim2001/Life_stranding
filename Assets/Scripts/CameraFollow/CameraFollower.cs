using Infastructure.Services.Input;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;

namespace CameraFollow
{
    public class CameraFollower : MonoBehaviour
    {
        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private Transform _target;
        private bool _isMouseRotating;

        private float _currentYRotation;
        private float _currentXRotation;

        private float _mouseSensitivity;

        private Vector3 _velocity;
        private Vector3 _offsetMovePosition;
        private IInputService _inputService;
        private IStaticDataService _staticDataService;


        [Inject]
        public void Construct(IInputService inputService, IStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;
            _inputService = inputService;
        }

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
            float scrollInput = _inputService.ScrollWheelAxis;

            if (scrollInput != 0f)
            {
                _mouseSensitivity -= scrollInput * SpiderStaticData.ScrollSensitivity;
                _mouseSensitivity = Mathf.Clamp(_mouseSensitivity, -2, 5f);
            }

            _offsetMovePosition = new Vector3(0, _mouseSensitivity, 0);
        }

        private void HandleMouse()
        {
            if (_inputService.RightMousePressed)
                _isMouseRotating = true;

            if (_inputService.RightMouseUp)
                _isMouseRotating = false;

            if (_isMouseRotating)
            {
                float mouseX = _inputService.MouseXAxis;
                float mouseY = _inputService.MouseYAxis;

                _currentYRotation += mouseX * SpiderStaticData.MouseSpeed * Time.deltaTime;
                //_currentXRotation += -mouseY * SpiderStaticData.MouseSpeed * Time.deltaTime;

                transform.rotation = Quaternion.Euler(_currentXRotation, _currentYRotation, 0);
            }
        }

        private void MoveToTarget()
        {
            Vector3 targetPosition = _target.position + _offsetMovePosition;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _velocity,
                SpiderStaticData.SmoothTime
            );
        }

        private void RotateToTarget()
        {
            _currentYRotation = transform.eulerAngles.y;
            //_currentXRotation = transform.eulerAngles.x;

            Quaternion targetRotation = Quaternion.Euler(_currentXRotation, _target.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                Time.fixedDeltaTime * SpiderStaticData.MouseRotationSpeed
            );
        }
    }
}