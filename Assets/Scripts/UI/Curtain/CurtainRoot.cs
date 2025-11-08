using DG.Tweening;
using UnityEngine;

namespace UI.Curtain
{
    public class CurtainRoot : MonoBehaviour, ICurtainRoot
    {
        private static readonly int StartTrigger = Animator.StringToHash("StartTrigger");

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Animator _animator;

        private Sequence _fadeSequence;
        private Tween _fadeTween;

        private void Awake() =>
            _animator.gameObject.SetActive(false);


        public void ShowAndHide()
        {
            _canvasGroup.alpha = 1;

            _fadeTween.Kill();
            _fadeTween = DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 0, 5).SetDelay(1);
        }

        public void Show()
        {
            if (Mathf.Approximately(_canvasGroup.alpha, 1))
                return;

            _canvasGroup.alpha = 1;
        }

        public void Hide()
        {
            if (_canvasGroup.alpha == 0)
                return;

            _fadeTween.Kill();
            _fadeTween = DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 0, 1);
        }

        private void ShowFlowerAnimation()
        {
            //_animator.gameObject.SetActive(true);
            //_animator.SetTrigger(StartTrigger);
        }
    }
}