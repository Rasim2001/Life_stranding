using System;
using UnityEngine;

namespace Common
{
    public class ObserverTrigger : MonoBehaviour
    {
        [SerializeField] private LayerMask _layerMask;

        public Action<Collider> OnTriggerEnterHappened;
        public Action<Collider> OnTriggerExitHappened;


        private void OnTriggerEnter(Collider other)
        {
            if (_layerMask != (_layerMask | (1 << other.gameObject.layer)))
                return;

            OnTriggerEnterHappened?.Invoke(other);
            
        }

        private void OnTriggerExit(Collider other)
        {
            if (_layerMask != (_layerMask | (1 << other.gameObject.layer)))
                return;

            OnTriggerExitHappened?.Invoke(other);
        }
    }
}