using System.Collections.Generic;
using System.Linq;
using Infastructure.StaticData.Product;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using SpiderController;
using SpiderController.Platform;
using SpiderController.StateMachine;
using UnityEngine;

namespace Infastructure.Services.PlatformObjects
{
    public class PlatformObjectsService : IPlatformObjectsService
    {
        private readonly IStaticDataService _staticData;
        private readonly List<PickupObjectBase> _objects = new List<PickupObjectBase>();
        public IReadOnlyList<PickupObjectBase> PickupObjects => _objects;
        private StateMachineData Data => _stateContext.Data;
        private Transform PlatformTransform => _stateContext.RotationPlaneTransform;
        private Rigidbody Rigidbody => _stateContext.Rigidbody;

        private PlatformSelector _platformSelector;
        private SpiderStateContext _stateContext;


        public PlatformObjectsService(IStaticDataService staticData) =>
            _staticData = staticData;

        public void Initialize(SpiderStateContext stateContext, PlatformSelector platformSelector)
        {
            _stateContext = stateContext;
            _platformSelector = platformSelector;
        }


        public void Add(PickupObjectBase obj)
        {
            if (_objects.Contains(obj))
                return;

            _objects.Add(obj);
            AddWeight(obj);

            obj.AttachToPlatform(PlatformTransform);
            _platformSelector.SetExcludeLayerMask();
        }

        public void Remove(PickupObjectBase obj)
        {
            if (!_objects.Contains(obj))
                return;

            _objects.Remove(obj);
            RemoveWeight(obj);

            _platformSelector.ReturnToDefaultMaterial();
            _platformSelector.ResetExcludeLayerMask();

            obj.DetachFromPlatform();
        }

        public void Throw(PickupObjectBase obj)
        {
            if (!_objects.Contains(obj))
                return;

            _objects.Remove(obj);
            RemoveWeight(obj);

            _platformSelector.ReturnToDefaultMaterial();
            _platformSelector.ResetExcludeLayerMask();

            obj.DetachFromPlatform();
            obj.ThrowObject();
        }

        public void ThrowAll()
        {
            if (_objects.Count == 0)
                return;

            _platformSelector.ReturnToDefaultMaterial();
            _platformSelector.ResetExcludeLayerMask();

            foreach (PickupObjectBase obj in _objects)
            {
                RemoveWeight(obj);

                obj.DetachFromPlatform();
                obj.ThrowObject();
            }

            _objects.Clear();
        }

        public void Update()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                PickupObjectBase obj = _objects[i];

                if (obj.IsOnPlatform)
                    obj.transform.localRotation =
                        Quaternion.Euler(obj.StartRotation.eulerAngles.x, 0, 0);

                if (obj.IsFreezingOnPlatform || obj.IsPuttingDown)
                    continue;

                if (IsPlatformUpsideDown())
                {
                    obj.GetChanceAttachToPlatform();
                    Remove(obj);

                    //obj.StartSimulatePhysics();

                    continue;
                }

                bool isOnPlatform = _platformSelector.IsOnPlatform(obj.Collider);

                if (isOnPlatform)
                    SimulateRotation(obj);
                else
                {
                    obj.GetChanceAttachToPlatform();
                    Remove(obj);

                    //obj.StartSimulatePhysics();
                }
            }
        }

        public void FixedUpdate()
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                PickupObjectBase obj = _objects[i];

                if (!obj.IsOnPlatform || obj.IsPuttingDown)
                    continue;

                if (obj.Rigidbody.constraints == RigidbodyConstraints.FreezeRotation)
                    obj.Rigidbody.linearVelocity = new Vector3(
                        Rigidbody.linearVelocity.x,
                        0,
                        Rigidbody.linearVelocity.z);
            }
        }

        public bool HasAny<T>() where T : PickupObjectBase =>
            _objects.Any(x => x is T);

        public T Get<T>() where T : PickupObjectBase =>
            _objects.FirstOrDefault(x => x is T) as T;

        public bool IsEmpty() => _objects.Count == 0;


        private void SimulateRotation(PickupObjectBase obj)
        {
            Vector3 platformRotation = PlatformTransform.eulerAngles;

            float angleX = Mathf.Deg2Rad * platformRotation.x;
            float angleZ = Mathf.Deg2Rad * platformRotation.z;

            Vector3 gravityForce = new Vector3(
                -Mathf.Sin(angleZ),
                0f,
                Mathf.Sin(angleX));

            Vector3 movementVector = gravityForce * (Time.deltaTime * obj.Speed);
            movementVector = obj.StartRotation * movementVector;

            obj.transform.Translate(movementVector, Space.Self);

            obj.transform.localPosition = new Vector3(
                obj.transform.localPosition.x,
                obj.StartPosition.y,
                obj.transform.localPosition.z);
        }

        private bool IsPlatformUpsideDown()
        {
            Vector3 worldUp = -Physics.gravity.normalized;
            float dot = Vector3.Dot(PlatformTransform.up, worldUp);
            return dot < Mathf.Epsilon;
        }

        private void AddWeight(PickupObjectBase obj)
        {
            if (obj.TryGetComponent(out IProduct product))
                Data.TotalWeight += GetWeight(product);
        }

        private void RemoveWeight(PickupObjectBase obj)
        {
            if (obj.TryGetComponent(out IProduct product))
                Data.TotalWeight -= GetWeight(product);
        }

        private float GetWeight(IProduct product)
        {
            ProductData productData =
                _staticData.ProductsStaticData.ProductsDictionary[product.ProductType];
            return productData.Weight;
        }
    }
}