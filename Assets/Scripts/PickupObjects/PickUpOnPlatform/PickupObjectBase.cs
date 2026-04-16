using Infastructure.Services.PlatformObjects;
using Infastructure.StaticData.Product;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.Rewind;
using UnityEngine;
using Zenject;

namespace PickupObjects.PickUpOnPlatform
{
    [RequireComponent(typeof(Rigidbody))]
    public class PickupObjectBase : MonoBehaviour, IProduct, IPickupRewindable
    {
        private const float PlatformLinearDamping = 10;
        private const float PlatformAngularDamping = 10;

        public ProductType ProductType { get; set; }
        public bool IsOnPlatform { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        public Collider Collider { get; private set; }

        public bool IsFreezingOnPlatform;
        public bool WasOnPlatform;
        public bool IsPuttingDown;

        public Vector3 StartPosition { get; private set; }
        public Quaternion StartRotation { get; private set; }
        public float Speed { get; private set; }

        private Vector3 _customPositionOffset = Vector3.zero;
        private float _defaultLinearDamping;
        private float _defaultAngularDamping;

        private IPlatformObjectsService _platformObjectsService;
        private IStaticDataService _staticDataService;

        [Inject]
        public void Construct(
            IPlatformObjectsService platformObjectsService,
            IStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;
            _platformObjectsService = platformObjectsService;
        }

        protected virtual void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            Collider = GetComponent<Collider>();

            _defaultLinearDamping = Rigidbody.linearDamping;
            _defaultAngularDamping = Rigidbody.angularDamping;
        }

        public virtual void Initialize()
        {
            ProductData productData =
                _staticDataService.ProductsStaticData.ProductsDictionary[ProductType];

            Speed = productData.Speed;
            StartPosition = productData.StartPositionVector;
            StartRotation = Quaternion.Euler(productData.StartRotationEuler);
        }

        public virtual PickupObjectSnapshot Capture()
        {
            return new PickupObjectSnapshot
            {
                Time = Time.fixedTime,
                Constraints = Rigidbody.constraints,
                UseGravity = Rigidbody.useGravity,
                AngularDamping = Rigidbody.angularDamping,
                LinearDamping = Rigidbody.linearDamping,
                WorldPosition = transform.position,
                WorldRotation = transform.rotation,
                LinearVelocity = Rigidbody.linearVelocity,
                AngularVelocity = Rigidbody.angularVelocity,
                IsKinematic = Rigidbody.isKinematic,
                ColliderEnabled = Collider.enabled,
                IsOnPlatform = IsOnPlatform,
                WasOnPlatform = WasOnPlatform,
                IsPuttingDown = IsPuttingDown,
                Parent = transform.parent,
            };
        }

        public virtual void ApplyLerp(PickupObjectSnapshot from, PickupObjectSnapshot to, float t)
        {
            transform.position = Vector3.Lerp(from.WorldPosition, to.WorldPosition, t);
            transform.rotation = Quaternion.Slerp(from.WorldRotation, to.WorldRotation, t);
        }

        public virtual void ApplyFinalSnapshot(PickupObjectSnapshot snapshot)
        {
            IsOnPlatform = snapshot.IsOnPlatform;
            WasOnPlatform = snapshot.WasOnPlatform;
            IsPuttingDown = snapshot.IsPuttingDown;

            Rigidbody.useGravity = snapshot.UseGravity;
            Rigidbody.angularDamping = snapshot.AngularDamping;
            Rigidbody.linearDamping = snapshot.LinearDamping;
            Rigidbody.constraints = snapshot.Constraints;
            Rigidbody.isKinematic = snapshot.IsKinematic;

            Collider.enabled = snapshot.ColliderEnabled;

            transform.SetParent(snapshot.Parent);
            transform.position = snapshot.WorldPosition;
            transform.rotation = snapshot.WorldRotation;

            if (!snapshot.IsKinematic)
            {
                Rigidbody.linearVelocity = snapshot.LinearVelocity;
                Rigidbody.angularVelocity = snapshot.AngularVelocity;
            }
        }

        public virtual void FreezeForRewind()
        {
            transform.SetParent(null);
            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.isKinematic = true;
        }


        public virtual void AttachToPlatform(Transform platformTransform)
        {
            if (!WasOnPlatform)
                WasOnPlatform = true;

            IsOnPlatform = true;

            Rigidbody.useGravity = false;
            Rigidbody.angularDamping = PlatformAngularDamping;
            Rigidbody.linearDamping = PlatformLinearDamping;
            Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            Rigidbody.isKinematic = false;

            transform.SetParent(platformTransform);
            transform.localPosition = StartPosition + _customPositionOffset;
            transform.localRotation = StartRotation;
        }


        public virtual void DetachFromPlatform()
        {
            IsOnPlatform = false;

            Rigidbody.useGravity = true;
            Rigidbody.angularDamping = _defaultAngularDamping;
            Rigidbody.linearDamping = _defaultLinearDamping;
            Rigidbody.constraints = RigidbodyConstraints.None;

            transform.SetParent(null);
        }

        public virtual void GetChanceAttachToPlatform()
        {
        }

        public virtual void ThrowObject() =>
            Rigidbody.linearVelocity = transform.up * 10;

        public void SetCustomOffsetPosition(Vector3 position) =>
            _customPositionOffset = position;

        protected virtual void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Default"))
                Rigidbody.constraints = IsOnPlatform
                    ? RigidbodyConstraints.FreezeRotation
                    : RigidbodyConstraints.None;
        }

        protected virtual void OnCollisionExit(Collision other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Default"))
                Rigidbody.constraints = IsOnPlatform
                    ? RigidbodyConstraints.FreezeAll
                    : RigidbodyConstraints.None;
        }
    }
}