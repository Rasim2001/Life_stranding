using Infastructure.Services.PlatformObjects;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;

namespace PickupObjects
{
    [RequireComponent(typeof(Rigidbody))]
    public class PickupObjectBase : MonoBehaviour
    {
        [SerializeField] private Bounds _platformBounds;
        [SerializeField] private float _speed = 1;
        [SerializeField] private float _colorLerpSpeed = 5;

        private readonly Vector3 _startPosition = new Vector3(0, 0.006f, 0);
        private readonly Quaternion _startRotation = Quaternion.Euler(-90, 0, 0);
        private Vector3 _customPositionOffset = Vector3.zero;

        public bool IsOnPlatform { get; private set; }
        public Rigidbody Rigidbody { get; private set; }

        public bool IsFreezingOnPlatform;

        private Material _robotPlaneMaterial;

        [SerializeField] private Transform _platform;
        private MeshRenderer _meshRenderer;
        private IPlatformObjectsService _platformObjectsService;

        [Inject]
        public void Construct(IStaticDataService staticDataService, IPlatformObjectsService platformObjectsService)
        {
            _platformObjectsService = platformObjectsService;
            _robotPlaneMaterial = new Material(staticDataService.MaterialsStaticData.RobotPlaneMaterial);
        }

        public void Initialize(Transform platformTransform, MeshRenderer meshRenderer)
        {
            _platform = platformTransform;
            _meshRenderer = meshRenderer;

            _meshRenderer.material = _robotPlaneMaterial;
        }

        private void Awake() =>
            Rigidbody = GetComponent<Rigidbody>();

        private void Update()
        {
            if (!IsOnPlatform || IsFreezingOnPlatform)
                return;

            Vector3 localPos = transform.localPosition;
            IsOnPlatform = _platformBounds.Contains(localPos);

            ChangeRobotPlaneColor(localPos);

            if (IsOnPlatform)
                SimulateRotation();
            else
                StartSimulatePhysics();
        }

        public void SetCustomOffsetPosition(Vector3 position) =>
            _customPositionOffset = position;


        protected virtual void StartSimulatePhysics()
        {
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

            _meshRenderer.material.color = Color.Lerp(
                _meshRenderer.material.color,
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

        public void OnDrawGizmosSelected()
        {
            if (_platform == null)
                return;

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = _platform.localToWorldMatrix;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_platformBounds.center, _platformBounds.size);

            Gizmos.matrix = oldMatrix;
        }
    }
}