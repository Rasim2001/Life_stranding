using Infastructure.Services.PlatformObjects;
using Infastructure.StaticData.Product;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;

namespace PickupObjects.PickUpOnPlatform
{
    [RequireComponent(typeof(Rigidbody))]
    public class PickupObjectBase : MonoBehaviour, IProduct
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