using Infastructure.Common.StableWorlUpManagement;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerInput.InputSourceRealization;
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

        private IInputService _inputService;
        private IStaticDataService _staticDataService;
        private IStableWorldUp _stableWorldUp;
        private ICutSceneService _cutSceneService;

        private Transform _target;
        private Vector3 _velocity;

        private bool _isMouseRotating;
        private float _mouseSensitivity;
        private CinemachineInputAxisController _axisController;
        private CameraSystem _cameraSystem;

        private float _yRotation;
        private float _xRotation;
        private float _cameraRotationSpeedX;
        private float _cameraRotationSpeedY;

        private float _cameraRotationSpeed;
        private JoystickInputSource _joystickInputSource;

        private float _defaultY;


        [Inject]
        public void Construct(
            IInputService inputService,
            IStaticDataService staticDataService,
            IStableWorldUp stableWorldUp,
            ICutSceneService cutSceneService)
        {
            _cutSceneService = cutSceneService;
            _stableWorldUp = stableWorldUp;
            _staticDataService = staticDataService;
            _inputService = inputService;
        }

        private void Awake() =>
            _mouseSensitivity = 2;

        public void Initialize(CameraSystem cameraSystem) =>
            _cameraSystem = cameraSystem;

        public void SetTarget(Transform spiderTransform) =>
            _target = spiderTransform;

        private void Start()
        {
            _defaultY = _cameraSystem.RotationComposer.Composition.ScreenPosition.y;

            _joystickInputSource = _inputService.GetInputSource<JoystickInputSource>();

            CinemachineCore.CameraUpdatedEvent.AddListener(UpdateAfterCinemachine);
        }

        private void OnDestroy() =>
            CinemachineCore.CameraUpdatedEvent.RemoveListener(UpdateAfterCinemachine);


        private void FixedUpdate()
        {
            if (_target == null)
                return;

            MoveToTarget();

            if (!_isMouseRotating)
                RotateToTarget();
        }

        private void UpdateAfterCinemachine(CinemachineBrain _)
        {
            if (_target == null || _cutSceneService.IsActive)
                return;

            HandleScrollWheel();
            HandleMouse();
            WorldUpRotate();
        }

        private void WorldUpRotate() =>
            _stableWorldUp.Rotate(_target.rotation);


        private void HandleScrollWheel()
        {
            float scrollInput = _inputService.ScrollWheelAxis;

            float maxLenght;
            if (_joystickInputSource == null)
                maxLenght = 7;
            else
                maxLenght = _joystickInputSource.IsGamepadActiveNow() ? 4 : 7;


            if (scrollInput != 0f)
            {
                _mouseSensitivity -= scrollInput * SpiderStaticData.ScrollSensitivity;
                _mouseSensitivity = Mathf.Clamp(_mouseSensitivity, 2, maxLenght);
            }

            float smoothSensitivityY = Mathf.Lerp(_cameraSystem.ThirdPersonFollow.ShoulderOffset.y, _mouseSensitivity,
                Time.deltaTime * 5);

            _cameraSystem.ThirdPersonFollow.ShoulderOffset = new Vector3(
                _cameraSystem.ThirdPersonFollow.ShoulderOffset.x,
                smoothSensitivityY,
                _cameraSystem.ThirdPersonFollow.ShoulderOffset.z);
        }

        private void HandleMouse()
        {
            if (_inputService.CenterMousePressed)
            {
                _isMouseRotating = true;

                _cameraRotationSpeed = SpiderStaticData.CameraRotationSpeed;
            }

            if (_inputService.CenterMouseUp)
            {
                _cameraRotationSpeed = SpiderStaticData.CameraRotationSpeed / 3;

                _yRotation = _defaultY;
                _xRotation = 0;

                _isMouseRotating = false;
            }


            if (_isMouseRotating)
            {
                float mouseXAxis = _inputService.MouseXAxis;
                float mouseYAxis = _inputService.MouseYAxis;

                _yRotation += mouseYAxis * SpiderStaticData.MouseRotationSpeedY * Time.deltaTime;
                _xRotation += mouseXAxis * SpiderStaticData.MouseRotationSpeedX * Time.deltaTime;
            }

            Vector2 rotationVector = new Vector2(-_xRotation, _yRotation);

            _cameraSystem.RotationComposer.Composition.ScreenPosition =
                Vector2.Lerp(_cameraSystem.RotationComposer.Composition.ScreenPosition, rotationVector,
                    _cameraRotationSpeed * Time.deltaTime);
        }


        private void MoveToTarget()
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                _target.position,
                ref _velocity,
                SpiderStaticData.SmoothTime
            );
        }

        private void RotateToTarget()
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                _target.rotation,
                Time.fixedDeltaTime * SpiderStaticData.CameraRotationSpeed
            );
        }
    }
}