using System;
using Infastructure.Services.Input;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using Unity.Cinemachine;
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

        private float _mouseSensitivity;

        private Vector3 _velocity;
        private Vector3 _offsetMovePosition;
        private IInputService _inputService;
        private IStaticDataService _staticDataService;

        private CinemachineBrain _cinemachineBrain;

        private void Awake() =>
            _cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();


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
            if (_inputService.CenterMousePressed)
                _isMouseRotating = true;

            if (_inputService.CenterMouseUp)
                _isMouseRotating = false;

            if (_isMouseRotating)
            {
                float mouseX = _inputService.MouseXAxis;

                _currentYRotation += mouseX * SpiderStaticData.MouseSpeed * Time.deltaTime;

                transform.rotation = Quaternion.Euler(0, _currentYRotation, 0);
            }
        }

        private void MoveToTarget()
        {
            Vector3 targetPosition = _target.position + _offsetMovePosition;

            transform.localPosition = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _velocity,
                SpiderStaticData.SmoothTime
            );
        }

        private void RotateToTarget()
        {
            _currentYRotation = transform.eulerAngles.y;

            Quaternion targetRotation =
                Quaternion.Euler(0, _target.eulerAngles.y, 0);

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                Time.fixedDeltaTime * SpiderStaticData.MouseRotationSpeed
            );
        }
    }
}