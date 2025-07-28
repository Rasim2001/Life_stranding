using _2;
using Infastructure.Services.Input;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;

namespace SpiderController
{
    public class SpiderPlane
    {
        private readonly IInputService _inputService;
        private readonly IStaticDataService _staticDataService;
        private readonly PlaneIndicator _planeIndicator;
        private readonly Transform _rotationPlaneTransform;

        private SpiderStaticData _spiderStaticData;

        private Vector2 _mouseInput;
        private Vector2 _initialMousePosition;
        private bool _isMouseHeld;


        public SpiderPlane(
            PlaneIndicator planeIndicator,
            Transform rotationPlaneTransform,
            IInputService inputService,
            IStaticDataService staticDataService)
        {
            _inputService = inputService;
            _staticDataService = staticDataService;
            _planeIndicator = planeIndicator;
            _rotationPlaneTransform = rotationPlaneTransform;
        }

        public void Initialize() =>
            _spiderStaticData = _staticDataService.SpiderStaticData;

        public void Update()
        {
            if (_spiderStaticData == null)
                return;

            if (_inputService.LeftMousePressed)
            {
                _planeIndicator.Show();
                _isMouseHeld = true;
                _initialMousePosition = Input.mousePosition;
            }
            else if (_inputService.LeftMouseUp)
            {
                _planeIndicator.Hide();
                _isMouseHeld = false;
                _mouseInput = Vector2.zero;
            }

            if (_isMouseHeld)
                HandleMousePosition();
        }

        public void FixedUpdate() =>
            ApplyRotation();

        private void HandleMousePosition()
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            _mouseInput.x = (mousePos.x - _initialMousePosition.x) / (screenSize.x / 2);
            _mouseInput.y = (mousePos.y - _initialMousePosition.y) / (screenSize.y / 2);

            _mouseInput = Vector2.ClampMagnitude(_mouseInput, 1f);

            _mouseInput.x = -_mouseInput.x;
            _mouseInput.y = -_mouseInput.y;

            _mouseInput *= _spiderStaticData.MouseSensitivity;
        }

        private void ApplyRotation()
        {
            if (_spiderStaticData == null)
                return;

            float targetAngleX = Mathf.Clamp(-_mouseInput.y * _spiderStaticData.MaxAngle, -_spiderStaticData.MaxAngle,
                _spiderStaticData.MaxAngle);
            float targetAngleZ = Mathf.Clamp(_mouseInput.x * _spiderStaticData.MaxAngle, -_spiderStaticData.MaxAngle,
                _spiderStaticData.MaxAngle);

            Quaternion targetLocalRotation = Quaternion.Euler(targetAngleX, 0f, targetAngleZ);

            _rotationPlaneTransform.localRotation = Quaternion.Slerp(
                _rotationPlaneTransform.localRotation,
                targetLocalRotation,
                Time.fixedDeltaTime * _spiderStaticData.RotationSpeed);
        }
    }
}