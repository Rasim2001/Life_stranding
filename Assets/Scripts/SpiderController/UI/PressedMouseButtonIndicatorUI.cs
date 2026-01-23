using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SpiderController.UI
{
    public class PressedMouseButtonIndicatorUI : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Color _pressedColor;

        private Tween _scaleTween;

        public void Show()
        {
            _scaleTween?.Kill();

            _scaleTween = _image.rectTransform.DOScale(new Vector3(1.1f, 1.1f, 1.1f), 0.25f);
            _image.color = Color.white;
        }

        public void Hide()
        {
            _scaleTween?.Kill();

            _scaleTween = _image.rectTransform.DOScale(Vector3.one, 0.25f);
            _image.color = _pressedColor;
        }

        private void OnDestroy() =>
            _scaleTween?.Kill();
    }
}