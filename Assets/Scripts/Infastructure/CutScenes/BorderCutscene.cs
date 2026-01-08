using DG.Tweening;
using UnityEngine;


namespace Infastructure.CutScenes
{
    public class BorderCutsceneUI : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Tween _moveTween;

        private void Awake() =>
            _rectTransform = GetComponent<RectTransform>();

        public void Play() =>
            _moveTween = _rectTransform.DOAnchorPosY(0, 0.25f).SetEase(Ease.OutCubic);

        private void OnDestroy() =>
            _moveTween?.Kill();
    }
}