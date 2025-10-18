using Infastructure.Services.PlatformObjects;
using Infastructure.StaticData.StaticDataService;
using SpiderController.Platform;
using UnityEngine;
using Zenject;

namespace PickupObjects.PickUpOnPlatform
{
    [RequireComponent(typeof(Rigidbody))]
    public class PickupObjectBase : MonoBehaviour
    {
        protected Vector3 StartPosition { get; set; }
        protected Quaternion StartRotation { get; set; }
        protected float Speed { get; set; }

        private Vector3 _customPositionOffset = Vector3.zero;

        public bool IsOnPlatform { get; private set; }
        public Rigidbody Rigidbody { get; private set; }

        public bool IsFreezingOnPlatform;

        private Transform _platformArmature;
        private IPlatformObjectsService _platformObjectsService;
        private PlatformSelector _platformSelector;
        private Collider _collider;

        [Inject]
        public void Construct(IStaticDataService staticDataService, IPlatformObjectsService platformObjectsService) =>
            _platformObjectsService = platformObjectsService;

        public virtual void Initialize(Transform platformTransform, PlatformSelector platformSelector)
        {
            _platformSelector = platformSelector;
            _platformArmature = platformTransform;
        }

        private void Awake()
        {
            _collider = GetComponent<Collider>();

            Rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (!IsOnPlatform || IsFreezingOnPlatform)
                return;

            IsOnPlatform = _platformSelector.IsOnPlatform(_collider);

            if (IsOnPlatform)
                SimulateRotation();
            else
                StartSimulatePhysics();
        }

        public void SetCustomOffsetPosition(Vector3 position) =>
            _customPositionOffset = position;


        protected virtual void StartSimulatePhysics()
        {
            _platformSelector.ReturnToDefaultMaterial();

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

            transform.SetParent(_platformArmature);

            transform.localPosition = StartPosition + _customPositionOffset;
            transform.localRotation = StartRotation;
        }


        private void SimulateRotation()
        {
            Vector3 platformRotation = _platformArmature.eulerAngles;

            int sing = StartRotation.x >= 0 ? 1 : -1;

            float angleX = Mathf.Deg2Rad * platformRotation.x;
            float angleZ = Mathf.Deg2Rad * platformRotation.z;

            Vector3 gravityForce = new Vector3(
                -Mathf.Sin(angleZ),
                0f,
                Mathf.Sin(angleX) * sing
            );

            Vector3 movementVector = gravityForce * (Time.deltaTime * Speed);
            movementVector = StartRotation * movementVector;

            transform.Translate(movementVector, Space.Self);
        }
    }
}