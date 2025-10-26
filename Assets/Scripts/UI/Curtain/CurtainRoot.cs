using DG.Tweening;
using UnityEngine;

namespace UI.Curtain
{
    public class CurtainRoot : MonoBehaviour, ICurtainRoot
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        private Sequence _fadeSequence;
        private Tween _fadeTween;

        public void ShowAndHide()
        {
            _canvasGroup.alpha = 1;

            _fadeTween.Kill();
            _fadeTween = DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 0, 5).SetDelay(1);
        }
    }
}