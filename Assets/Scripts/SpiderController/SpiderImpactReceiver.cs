using SpiderController.StateMachine;
using UnityEngine;

namespace SpiderController
{
    public class SpiderImpactReceiver
    {
        private readonly SpiderStateContext _stateContext;
        private StateMachineData Data => _stateContext.Data;
        private Transform Transform => _stateContext.Transform;

        private const float ExplosionDecayRate = 5f;
        private const float MinThreshold = 0.1f;

        private const float AngularDecayRate = 2f;
        private const float AngularForceMultiplier = 0.5f;

        public SpiderImpactReceiver(SpiderStateContext stateContext) =>
            _stateContext = stateContext;

        public void ApplyExplosionForce(Vector3 explosionPosition, float force, float radius)
        {
            Vector3 direction = (Transform.position - explosionPosition).normalized;

            float distance = Vector3.Distance(Transform.position, explosionPosition);
            float distanceFactor = Mathf.Clamp01(1f - distance / radius);

            Vector3 torqueAxis = Vector3.Cross(direction, Transform.up).normalized;

            Data.ExplosionAngularVector = torqueAxis * (force * distanceFactor * AngularForceMultiplier);
            Data.ExplosionVector = direction * (force * distanceFactor);
        }

        public void Update()
        {
            Data.ExplosionVector =
                DampVector(Data.ExplosionVector, ExplosionDecayRate);

            Data.ExplosionAngularVector =
                DampVector(Data.ExplosionAngularVector, AngularDecayRate);
        }

        private Vector3 DampVector(Vector3 value, float decayRate)
        {
            return value.magnitude > MinThreshold
                ? Vector3.Lerp(value, Vector3.zero, Time.deltaTime * decayRate)
                : Vector3.zero;
        }
    }
}