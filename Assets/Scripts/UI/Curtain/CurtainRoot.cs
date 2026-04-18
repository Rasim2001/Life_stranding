using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
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
        private Tween _showTween;

        private void Awake() =>
            _animator.gameObject.SetActive(false);


        public void ShowAndHide()
        {
            _canvasGroup.alpha = 0;

            _fadeSequence?.Kill();
            _fadeSequence = DOTween.Sequence();

            _fadeTween?.Kill();
            _showTween?.Kill();

            _showTween = DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 1, 2).SetDelay(0.5f);
            _fadeTween = DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 0, 2).SetDelay(2);

            _fadeSequence
                .Append(_showTween)
                .Append(_fadeTween);
        }

        public void FandeIn(float time)
        {
            _canvasGroup.alpha = 1;

            _fadeTween?.Kill();

            _fadeTween = DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 0, time).SetDelay(0.25f);
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

            _fadeTween?.Kill();
            _fadeTween = DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 0, 1).SetDelay(1);
        }

        public bool IsShowing =>
            _fadeSequence != null && _fadeSequence.IsPlaying() || _fadeTween != null && _fadeTween.IsPlaying();

        private void ShowFlowerAnimation()
        {
            //_animator.gameObject.SetActive(true);
            //_animator.SetTrigger(StartTrigger);
        }
    }
}