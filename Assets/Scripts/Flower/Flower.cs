using System;
using HUD;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;

namespace SpiderController
{
    [RequireComponent(typeof(Rigidbody))]
    public class Flower : MonoBehaviour
    {
        [SerializeField] private Transform _platform;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private Bounds _platformBounds;
        [SerializeField] private float _speed = 1;
        [SerializeField] private float _colorLerpSpeed = 5;
        public Rigidbody Rigidbody => _rigidbody;
        public bool IsOnPlatform => _isOnPlatform;

        public bool IsFreezingOnPlatform;

        private bool _isOnPlatform = true;
        private Rigidbody _rigidbody;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private FlowerPointIndicator _flowerPointIndicator;

        private Material _robotPlaneMaterial;


        [Inject]
        public void Construct(IStaticDataService staticDataService)
        {
            _robotPlaneMaterial = new Material(staticDataService.MaterialsStaticData.RobotPlaneMaterial);
            _meshRenderer.material = _robotPlaneMaterial;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _startPosition = transform.localPosition;
            _startRotation = transform.localRotation;
        }

        public void Initialize(FlowerPointIndicator flowerPointIndicator) =>
            _flowerPointIndicator = flowerPointIndicator;

        private void Update()
        {
            if (!_isOnPlatform || IsFreezingOnPlatform)
                return;

            Vector3 localPos = transform.localPosition;
            _isOnPlatform = _platformBounds.Contains(localPos);

            ChangeRobotPlaneColor(localPos);

            if (_isOnPlatform)
                SimulateRotation();
            else
                SimulatePhysics();
        }

        private void ChangeRobotPlaneColor(Vector3 localPos)
        {
            Vector3 center = _platformBounds.center;
            Vector3 extents = _platformBounds.extents;

            float normalizedX = Mathf.Abs(localPos.x - center.x) / extents.x;
            float normalizedZ = Mathf.Abs(localPos.z - center.z) / extents.z;

            float distanceFactor = Mathf.Clamp01(Mathf.Max(normalizedX, normalizedZ));

            Color targetColor;
            if (distanceFactor < 0.33f)
                targetColor = Color.blue;
            else if (distanceFactor < 0.66f)
                targetColor = new Color(0.5f, 0f, 0.5f);
            else
                targetColor = Color.red;

            _robotPlaneMaterial.color = Color.Lerp(
                _robotPlaneMaterial.color,
                targetColor,
                Time.deltaTime * _colorLerpSpeed
            );
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

        public void ResetSimulate()
        {
            transform.SetParent(_platform);
            transform.localPosition = _startPosition;
            transform.localRotation = _startRotation;

            _rigidbody.isKinematic = true;
            _isOnPlatform = true;

            _flowerPointIndicator.HideTargetPoint();
        }

        private void SimulatePhysics()
        {
            transform.SetParent(null);

            _isOnPlatform = false;
            _rigidbody.isKinematic = false;

            _flowerPointIndicator.ShowTargetPoint();
        }

        private void OnDrawGizmosSelected()
        {
            if (_platform == null) return;

            // Сохраняем текущую матрицу трансформации Gizmos
            Matrix4x4 oldMatrix = Gizmos.matrix;

            // Устанавливаем матрицу трансформации платформы
            Gizmos.matrix = _platform.localToWorldMatrix;

            // Рисуем bounds в локальных координатах платформы
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_platformBounds.center, _platformBounds.size);

            // Дополнительно можно нарисовать центр bounds
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_platformBounds.center, 0.1f);

            // Восстанавливаем исходную матрицу
            Gizmos.matrix = oldMatrix;
        }
    }
}