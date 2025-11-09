using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Services.Hint;
using Sirenix.OdinInspector;
using TMPro;
using UI;
using UnityEngine;
using Zenject;

namespace HUD
{
    public class Hint : MonoBehaviour
    {
        [SerializeField] private RectTransform _container;
        [SerializeField] private FramePiecesUI _framePiecesUI;

        [SerializeField] private Ease _easyShow = Ease.Linear;
        [SerializeField] private Ease _easyHide = Ease.Linear;

        [SerializeField] private TextMeshProUGUI _description;


        private Coroutine _coroutine;
        private Tween _containerTween;

        private IHintService _hintService;

        [Inject]
        public void Construct(IHintService hintService) =>
            _hintService = hintService;

        private void Start()
        {
            _hintService.OnProductHint += ShowProduct;
            _hintService.OnCheckpointHint += ShowCheckpointHint;
            _hintService.OnGeneratorHint += ShowGeneratorHint;
        }

        private void OnDestroy()
        {
            _hintService.OnProductHint -= ShowProduct;
            _hintService.OnCheckpointHint -= ShowCheckpointHint;
            _hintService.OnGeneratorHint -= ShowGeneratorHint;

            if (_coroutine != null)
                StopCoroutine(_coroutine);

            _containerTween?.Kill();
        }

        private void ShowProduct()
        {
            if (_coroutine != null)
                return;

            _description.text = "Одновременно можно брать только один тип носителя";

            Show();
        }

        private void ShowCheckpointHint()
        {
            if (_coroutine != null)
                return;

            _description.text = "Чекпоинт работает только с колбой";

            Show();
        }

        private void ShowGeneratorHint()
        {
            if (_coroutine != null)
                return;

            _description.text = "Генератор нужно активировать с перемычкой";

            Show();
        }


        [Button]
        private void Show() =>
            _coroutine = StartCoroutine(ShowCoroutine());

        private IEnumerator ShowCoroutine()
        {
            _framePiecesUI.MoveFramePiecesAsync().Forget();
            _containerTween = _container.DOAnchorPosX(775f, 0.25f).SetEase(_easyShow);

            yield return new WaitForSeconds(2f);

            Hide();
        }


        private void Hide()
        {
            _containerTween = _container.DOAnchorPosX(1120, 0.25f).SetEase(_easyHide)
                .OnComplete(() =>
                {
                    _framePiecesUI.ResetFramePieces();

                    StopCoroutine(_coroutine);
                    _coroutine = null;
                });
        }
    }
}