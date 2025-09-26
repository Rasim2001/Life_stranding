using UnityEngine;

namespace SpiderController.Scanner
{
    public class ScannerAnimator : MonoBehaviour
    {
        private const string ScannerAnimation = "ScannerAnimation";

        private Animator _animator;

        private void Awake() =>
            _animator = GetComponent<Animator>();

        public void PlayScanAnimation() =>
            _animator.Play(ScannerAnimation, 0, 0);
    }
}