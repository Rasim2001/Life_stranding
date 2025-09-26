using UnityEngine;

namespace SpiderController.Thruster
{
    public class ThrusterSystem : MonoBehaviour
    {
        [SerializeField] private ThrusterAnimator _animator;
        [SerializeField] private ParticleSystem _particleSystem;

        public void Open(bool value)
        {
            if (value)
                _particleSystem.Play(true);
            else
                _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            _animator.Open(value);
        }
    }
}