using SpiderController.StateMachine;
using UnityEngine;

namespace SpiderController
{
    public class SpiderImpactReceiver
    {
        private readonly StateMachineData _stateMachineData;
        private readonly Transform _spiderTransform;

        private const float ExplosionDecayRate = 5f;
        private const float MinThreshold = 0.1f;

        private const float AngularDecayRate = 2f;
        private const float AngularForceMultiplier = 0.5f;

        public SpiderImpactReceiver(StateMachineData stateMachineData, Transform spiderTransform)
        {
            _stateMachineData = stateMachineData;
            _spiderTransform = spiderTransform;
        }

        public void ApplyExplosionForce(Vector3 explosionPosition, float force, float radius)
        {
            Vector3 direction = (_spiderTransform.position - explosionPosition).normalized;

            float distance = Vector3.Distance(_spiderTransform.position, explosionPosition);
            float distanceFactor = Mathf.Clamp01(1f - distance / radius);

            Vector3 torqueAxis = Vector3.Cross(direction, _spiderTransform.up).normalized;

            _stateMachineData.ExplosionAngularVector = torqueAxis * (force * distanceFactor * AngularForceMultiplier);
            _stateMachineData.ExplosionVector = direction * (force * distanceFactor);
        }

        public void Update()
        {
            _stateMachineData.ExplosionVector =
                DampVector(_stateMachineData.ExplosionVector, ExplosionDecayRate);

            _stateMachineData.ExplosionAngularVector =
                DampVector(_stateMachineData.ExplosionAngularVector, AngularDecayRate);
        }

        private Vector3 DampVector(Vector3 value, float decayRate)
        {
            return value.magnitude > MinThreshold
                ? Vector3.Lerp(value, Vector3.zero, Time.deltaTime * decayRate)
                : Vector3.zero;
        }
    }
}