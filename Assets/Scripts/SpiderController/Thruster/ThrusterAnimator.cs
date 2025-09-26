using UnityEngine;

namespace SpiderController.Thruster
{
    public class ThrusterAnimator : MonoBehaviour
    {
        private static readonly int IsOpenedHash = Animator.StringToHash("IsOpened");

        private Animator _animator;

        private void Awake() =>
            _animator = GetComponent<Animator>();

        public void Open(bool value) =>
            _animator.SetBool(IsOpenedHash, value);
    }
}