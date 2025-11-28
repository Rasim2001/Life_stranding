using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UI;
using UnityEngine;

namespace Hints
{
    public abstract class HintBase : MonoBehaviour
    {
        [SerializeField] private RectTransform _container;
        [SerializeField] private FramePiecesUI _framePiecesUI;

        [SerializeField] private Ease _easyShow = Ease.Linear;
        [SerializeField] private Ease _easyHide = Ease.Linear;

        protected RectTransform Container => _container;

        private Coroutine _coroutine;
        private Tween _containerTween;

        private float _defaultAnchorPositionX;

        protected virtual void Start() =>
            _defaultAnchorPositionX = _container.anchoredPosition.x;

        protected virtual void OnDestroy()
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            _containerTween?.Kill();
        }


        protected void Show(float showTime, float anchorPositionX)
        {
            if (_coroutine != null)
                return;

            _coroutine = StartCoroutine(ShowCoroutine(showTime, anchorPositionX));
        }

        private IEnumerator ShowCoroutine(float showTime, float anchorPositionX)
        {
            _framePiecesUI.MoveFramePiecesAsync().Forget();
            _containerTween = _container.DOAnchorPosX(anchorPositionX, 0.25f).SetEase(_easyShow);

            yield return new WaitForSeconds(showTime);

            Hide();
        }


        private void Hide()
        {
            _containerTween = _container.DOAnchorPosX(_defaultAnchorPositionX, 0.25f).SetEase(_easyHide)
                .OnComplete(() =>
                {
                    _framePiecesUI.ResetFramePieces();

                    StopCoroutine(_coroutine);
                    _coroutine = null;
                });
        }
    }
}