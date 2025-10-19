using System;
using Infastructure.Services.PlatformObjects;
using Infastructure.StaticData.StaticDataService;
using SpiderController;
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
        private Transform _spiderTransform;


        [Inject]
        public void Construct(IStaticDataService staticDataService, IPlatformObjectsService platformObjectsService) =>
            _platformObjectsService = platformObjectsService;

        public virtual void Initialize(Transform platformTransform, PlatformSelector platformSelector)
        {
            _platformSelector = platformSelector;
            _platformArmature = platformTransform;

            _spiderTransform = _platformArmature.GetComponentInParent<Spider>().transform;
        }

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
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


        public virtual void StopSimulatePhysics()
        {
            if (!_platformObjectsService.PickupObjects.Contains(this))
                _platformObjectsService.PickupObjects.Add(this);

            Rigidbody.useGravity = false;
            IsOnPlatform = true;

            _platformSelector.SetExcludeLayerMask();

            transform.SetParent(_platformArmature);

            transform.localPosition = StartPosition + _customPositionOffset;
            transform.localRotation = StartRotation;
        }


        protected virtual void StartSimulatePhysics()
        {
            _platformSelector.ReturnToDefaultMaterial();

            _platformObjectsService.PickupObjects.Remove(this);

            Rigidbody.useGravity = true;
            IsOnPlatform = false;

            _platformSelector.ResetExcludeLayerMask();

            transform.SetParent(null);
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

            transform.localPosition =
                new Vector3(transform.localPosition.x, StartPosition.y, transform.localPosition.z);
        }
    }
}