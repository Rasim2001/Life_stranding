using System;
using Infastructure.Services.PlatformObjects;
using Infastructure.StaticData.StaticDataService;
using SpiderController;
using SpiderController.Platform;
using UnityEngine;
using VInspector.Libs;
using Zenject;

namespace PickupObjects.PickUpOnPlatform
{
    [RequireComponent(typeof(Rigidbody))]
    public class PickupObjectBase : MonoBehaviour
    {
        [SerializeField] private Vector3 _startRotationEuler;
        [SerializeField] private Vector3 _startPositionVector;

        [SerializeField] private float _speed = 1;
        private Vector3 StartPosition => _startPositionVector;
        private Quaternion StartRotation => Quaternion.Euler(_startRotationEuler);

        private Vector3 _customPositionOffset = Vector3.zero;

        public bool IsOnPlatform { get; private set; }
        public Rigidbody Rigidbody { get; private set; }

        public bool IsFreezingOnPlatform;

        private Transform _platformArmature;
        private IPlatformObjectsService _platformObjectsService;
        private PlatformSelector _platformSelector;

        [Inject]
        public void Construct(IStaticDataService staticDataService, IPlatformObjectsService platformObjectsService) =>
            _platformObjectsService = platformObjectsService;

        public void Initialize(Transform platformTransform, PlatformSelector platformSelector)
        {
            _platformSelector = platformSelector;
            _platformArmature = platformTransform;
        }

        private void Awake() =>
            Rigidbody = GetComponent<Rigidbody>();

        private void Update()
        {
            if (!IsOnPlatform || IsFreezingOnPlatform)
                return;

            IsOnPlatform = _platformSelector.IsOnPlatform(transform.position);

            if (IsOnPlatform)
                SimulateRotation();
            else
                StartSimulatePhysics();
        }

        public void SetCustomOffsetPosition(Vector3 position) =>
            _customPositionOffset = position;


        protected virtual void StartSimulatePhysics()
        {
            Debug.Log("Start");

            _platformSelector.ReturnToDefaultMaterial();

            _platformObjectsService.PickupObjects.Remove(this);

            Rigidbody.isKinematic = false;
            IsOnPlatform = false;

            transform.SetParent(null);
        }

        public virtual void StopSimulatePhysics()
        {
            Debug.Log("Stop");

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

            float angleX = Mathf.Deg2Rad * platformRotation.x;
            float angleZ = Mathf.Deg2Rad * platformRotation.z;

            Vector3 gravityForce = new Vector3(
                -Mathf.Sin(angleZ),
                0f,
                -Mathf.Sin(angleX)
            );

            Vector3 movementVector = gravityForce * (Time.deltaTime * _speed);
            movementVector = StartRotation * movementVector;

            transform.Translate(movementVector, Space.Self);
        }
    }
}