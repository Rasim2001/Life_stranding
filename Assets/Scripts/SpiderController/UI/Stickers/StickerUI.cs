using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SpiderController.UI.Stickers
{
    public class StickerUI : MonoBehaviour
    {
        [SerializeField] private Image _stickerImage;

        private Coroutine _coroutine;
        private readonly float _timeShow = 1;

        private void Start() =>
            _stickerImage.transform.localScale = Vector3.zero;

        public void PlaySticker(StickerEnum sticker)
        {
            switch (sticker)
            {
                case StickerEnum.FallingDown:
                    _stickerImage.color = Color.red;
                    break;
                case StickerEnum.StartGame:
                    _stickerImage.color = Color.green;
                    break;
            }


            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }

            _coroutine = StartCoroutine(PlayCoroutine());
        }

        private IEnumerator PlayCoroutine()
        {
            Show();

            yield return new WaitForSeconds(_timeShow);

            Hide();
        }


        private void Show() =>
            _stickerImage.transform.DOScale(Vector3.one, _timeShow);

        private void Hide() =>
            _stickerImage.transform.DOScale(Vector3.zero, _timeShow);
    }
}