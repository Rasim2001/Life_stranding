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
        private readonly float _linearDamping = 10;
        private readonly float _angularDamping = 10;

        public bool IsOnPlatform { get; private set; }
        public Rigidbody Rigidbody { get; private set; }

        public bool IsFreezingOnPlatform;

        protected Vector3 StartPosition { get; set; }
        protected Quaternion StartRotation { get; set; }
        protected float Speed { get; set; }

        private Vector3 _customPositionOffset = Vector3.zero;

        private Transform _platformArmature;
        private IPlatformObjectsService _platformObjectsService;
        private PlatformSelector _platformSelector;

        private Collider _collider;
        private Rigidbody _spiderRigidbody;


        private float _linearDefaultDamping;
        private float _angularDefaultDamping;


        [Inject]
        public void Construct(IStaticDataService staticDataService, IPlatformObjectsService platformObjectsService) =>
            _platformObjectsService = platformObjectsService;

        public virtual void Initialize(Transform platformTransform, PlatformSelector platformSelector)
        {
            _platformSelector = platformSelector;
            _platformArmature = platformTransform;

            _spiderRigidbody = _platformArmature.GetComponentInParent<Spider>().Rigidbody;
        }

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();

            _linearDefaultDamping = Rigidbody.linearDamping;
            _angularDefaultDamping = Rigidbody.angularDamping;

            _collider = GetComponent<Collider>();
        }

        private void Update()
        {
            if (IsOnPlatform)
                transform.localRotation = Quaternion.Euler(StartRotation.eulerAngles.x, 0, 0);

            if (!IsOnPlatform || IsFreezingOnPlatform)
                return;

            IsOnPlatform = _platformSelector.IsOnPlatform(_collider);

            if (IsOnPlatform)
                SimulateRotation();
            else
                StartSimulatePhysics();
        }

        private void FixedUpdate()
        {
            if (!IsOnPlatform)
                return;

            if (Rigidbody.constraints == RigidbodyConstraints.FreezeRotation)
                Rigidbody.linearVelocity =
                    new Vector3(_spiderRigidbody.linearVelocity.x, 0, _spiderRigidbody.linearVelocity.z);
        }

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Default"))
                Rigidbody.constraints = IsOnPlatform ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.None;
        }

        private void OnCollisionExit(Collision other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Default"))
                Rigidbody.constraints = IsOnPlatform ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
        }

        public void SetCustomOffsetPosition(Vector3 position) =>
            _customPositionOffset = position;


        public virtual void StopSimulatePhysics()
        {
            if (!_platformObjectsService.PickupObjects.Contains(this))
                _platformObjectsService.PickupObjects.Add(this);

            IsOnPlatform = true;
            Rigidbody.useGravity = false;
            Rigidbody.angularDamping = _angularDamping;
            Rigidbody.linearDamping = _linearDamping;
            Rigidbody.constraints = RigidbodyConstraints.FreezeAll;

            _platformSelector.SetExcludeLayerMask();

            transform.SetParent(_platformArmature);

            transform.localPosition = StartPosition + _customPositionOffset;
            transform.localRotation = StartRotation;
        }


        protected virtual void StartSimulatePhysics()
        {
            Debug.Log("StartSimulatePhysics");

            _platformSelector.ReturnToDefaultMaterial();

            _platformObjectsService.PickupObjects.Remove(this);

            IsOnPlatform = false;
            Rigidbody.useGravity = true;
            Rigidbody.angularDamping = _angularDefaultDamping;
            Rigidbody.linearDamping = _linearDefaultDamping;
            Rigidbody.constraints = RigidbodyConstraints.None;

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