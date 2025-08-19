using System;
using UnityEngine;

namespace Common
{
    public class ObserverTrigger : MonoBehaviour
    {
        [SerializeField] private LayerMask _layerMask;

        public Action OnTriggerEnterHappened;
        public Action OnTriggerExitHappened;

        public Collider Collider { get; private set; }

        private void OnTriggerEnter(Collider other)
        {
            if (_layerMask != (_layerMask | (1 << other.gameObject.layer)))
                return;

            Collider = other;
            OnTriggerEnterHappened?.Invoke();
        }

        private void OnTriggerExit(Collider other)
        {
            if (_layerMask != (_layerMask | (1 << other.gameObject.layer)))
                return;

            Collider = other;
            OnTriggerExitHappened?.Invoke();
        }
    }
}