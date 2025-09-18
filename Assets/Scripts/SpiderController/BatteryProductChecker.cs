using System;
using System.Collections.Generic;
using Common;
using UnityEngine;

namespace SpiderController
{
    public class BatteryProductChecker : MonoBehaviour
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


        private void TriggerEnter(Collider obj)
        {
            if (!Results.Contains(obj))
                Results.Add(obj);
        }

        private void TriggerExit(Collider obj)
        {
            if (Results.Contains(obj))
            {
                Results.Remove(obj);

                OnRemoveHappened?.Invoke(obj);
            }
        }
    }
}