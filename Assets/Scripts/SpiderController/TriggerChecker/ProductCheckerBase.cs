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

        public Action<Collider> OnRemoveHappened;

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

        protected virtual bool Accept(Collider col) => false;

        protected bool TryGetProduct(Collider col, ProductType type)
        {
            IProduct product = col.GetComponent<IProduct>();

            if (product != null)
                return product.ProductType == type;

            return false;
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