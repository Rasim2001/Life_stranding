using Infastructure.Services.Ability;
using PickupObjects;
using UnityEngine;
using Zenject;

namespace Common
{
    public class LaboratoryDestroyableObject : MonoBehaviour
    {
        [SerializeField] private Rigidbody[] _allRigidbodies;
        [SerializeField] private Transform _impulsePoint;

        [Header("Impulse")]
        [SerializeField, Min(0f)] private float _forwardImpulse = 2.5f;

        private IAbilityService _abilityService;

        [Inject]
        public void Construct(IAbilityService abilityService) =>
            _abilityService = abilityService;

        private void Start() =>
            _abilityService.OnAbilityAddHappened += DestroyObjects;

        private void OnDestroy() =>
            _abilityService.OnAbilityAddHappened -= DestroyObjects;

        private void DestroyObjects(ProductType type)
        {
            if (type != ProductType.Flower)
                return;

            foreach (Rigidbody rb in _allRigidbodies)
            {
                rb.isKinematic = false;

                Vector3 direction = rb.transform.position - _impulsePoint.position;
                Vector3 impulse = direction * _forwardImpulse;

                rb.AddForce(impulse, ForceMode.Impulse);
            }
        }
    }
}