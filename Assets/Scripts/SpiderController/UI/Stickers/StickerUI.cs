using System;
using System.Collections;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

namespace SpiderController.UI.Stickers
{
    public class StickerUI : MonoBehaviour
    {
        [SerializeField] private Image _stickerImage;

        private Coroutine _coroutine;
        private Tween _tweenScale;
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
            _tweenScale = _stickerImage.transform.DOScale(Vector3.one, _timeShow);

        private void Hide() =>
            _tweenScale = _stickerImage.transform.DOScale(Vector3.zero, _timeShow);

        private void OnDestroy() =>
            _tweenScale.Kill();
    }
}