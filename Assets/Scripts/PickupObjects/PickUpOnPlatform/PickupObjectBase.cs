using Infastructure.Services.PlatformObjects;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;

namespace PickupObjects.PickUpOnPlatform
{
    [RequireComponent(typeof(Rigidbody))]
    public class PickupObjectBase : MonoBehaviour
    {
        [SerializeField] private float _speed = 1;

        private readonly Vector3 _startPosition = new Vector3(0, 0.007330549f, 0);
        private readonly Quaternion _startRotation = Quaternion.Euler(-90, 0, 0);
        private Vector3 _customPositionOffset = Vector3.zero;

        public bool IsOnPlatform { get; private set; }
        public Rigidbody Rigidbody { get; private set; }

        public bool IsFreezingOnPlatform;

        private Material _planeBlinkMaterial;
        private Material _defaultMaterial;

        private bool _isBlinking;
        private float _blinkSpeed;

        private Transform _platform;
        private SkinnedMeshRenderer _meshRenderer;
        private IPlatformObjectsService _platformObjectsService;
        private SphereCollider _sphereCollider;

        [Inject]
        public void Construct(IStaticDataService staticDataService, IPlatformObjectsService platformObjectsService)
        {
            _platformObjectsService = platformObjectsService;
            _planeBlinkMaterial = new Material(staticDataService.MaterialsStaticData.PlaneBlinkMaterial);
        }

        public void Initialize(Transform platformTransform, SkinnedMeshRenderer meshRenderer,
            SphereCollider sphereCollider)
        {
            _sphereCollider = sphereCollider;
            _platform = platformTransform;
            _meshRenderer = meshRenderer;

            _defaultMaterial = _meshRenderer.material;
        }

        private void Awake() =>
            Rigidbody = GetComponent<Rigidbody>();

        private void Update()
        {
            if (!IsOnPlatform || IsFreezingOnPlatform)
                return;

            Vector3 localPos = transform.localPosition;
            float distanceFactor = Mathf.Max(Mathf.Abs(localPos.x), Mathf.Abs(localPos.z));
            float distanceFactorNormalized = distanceFactor / _sphereCollider.radius;


            IsOnPlatform = distanceFactor < _sphereCollider.radius;


            if (IsOnPlatform)
            {
                ChangeRobotPlaneColor(distanceFactorNormalized);
                SimulateRotation();
            }

            else
                StartSimulatePhysics();
        }

        public void SetCustomOffsetPosition(Vector3 position) =>
            _customPositionOffset = position;


        protected virtual void StartSimulatePhysics()
        {
            ReturnToDefaultMaterial();

            _platformObjectsService.PickupObjects.Remove(this);

            Rigidbody.isKinematic = false;
            IsOnPlatform = false;

            transform.SetParent(null);
        }

        public virtual void StopSimulatePhysics()
        {
            if (!_platformObjectsService.PickupObjects.Contains(this))
                _platformObjectsService.PickupObjects.Add(this);

            Rigidbody.isKinematic = true;
            IsOnPlatform = true;

            transform.SetParent(_platform);

            transform.localPosition = _startPosition + _customPositionOffset;
            transform.localRotation = _startRotation;
        }


        private void ChangeRobotPlaneColor(float distanceFactor)
        {
            if (distanceFactor > 0.5f)
                SetBlinkMaterial();
            else
                ReturnToDefaultMaterial();
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

        private void ReturnToDefaultMaterial()
        {
            if (!_isBlinking)
                return;

            _isBlinking = false;
            _meshRenderer.material = _defaultMaterial;
        }

        private void SetBlinkMaterial()
        {
            if (_isBlinking)
                return;

            _isBlinking = true;
            _meshRenderer.material = _planeBlinkMaterial;
        }
    }
}