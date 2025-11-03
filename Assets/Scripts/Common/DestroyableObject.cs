using Sirenix.Utilities;
using UnityEngine;

namespace Common
{
    public class DestroyableObject : MonoBehaviour
    {
        [SerializeField] private Rigidbody[] _allRigidbodies;
        [SerializeField] private ObserverTrigger _observerTrigger;

        private void Start() =>
            _observerTrigger.OnTriggerEnterHappened += TriggerHappened;

        private void OnDestroy() =>
            _observerTrigger.OnTriggerEnterHappened -= TriggerHappened;

        private void TriggerHappened(Collider obj)
        {
            Debug.Log("Trigger");

            _allRigidbodies.ForEach(x => x.isKinematic = false);
        }
    }
}