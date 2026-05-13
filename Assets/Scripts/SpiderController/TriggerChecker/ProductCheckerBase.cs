using System;
using System.Collections.Generic;
using Common;
using PickupObjects;
using UnityEngine;

namespace SpiderController.TriggerChecker
{
    public class ProductCheckerBase : MonoBehaviour
    {
        [SerializeField] private ObserverTrigger _observerTrigger;
        public event Action<Collider> OnRemoveHappened;

        public List<Collider> Results = new List<Collider>();

        private void Start()
        {
            _observerTrigger.OnTriggerEnterHappened += TriggerEnter;
            _observerTrigger.OnTriggerExitHappened += TriggerExit;
        }

        private void OnDestroy()
        {
            _observerTrigger.OnTriggerEnterHappened -= TriggerEnter;
            _observerTrigger.OnTriggerExitHappened -= TriggerExit;
        }

        public void ForceRemove(Collider col)
        {
            if (Results.Contains(col))
            {
                Results.Remove(col);

                OnRemoveHappened?.Invoke(col);
            }
        }

        protected virtual bool Accept(Collider col) => false;

        protected bool TryGetProduct(Collider col, ProductType type)
        {
            IProduct product = col.GetComponent<IProduct>();

            if (product != null)
                return product.ProductType == type;

            return false;
        }

        protected bool TryGetProduct(Collider col, Type type)
        {
            bool value = col.GetComponent(type) != null;

            return value;
        }

        private void TriggerEnter(Collider obj)
        {
            if (!Accept(obj))
                return;

            if (!Results.Contains(obj))
                Results.Add(obj);
        }

        private void TriggerExit(Collider obj)
        {
            if (!Accept(obj))
                return;

            if (Results.Contains(obj))
            {
                Results.Remove(obj);

                OnRemoveHappened?.Invoke(obj);
            }
        }
    }
}