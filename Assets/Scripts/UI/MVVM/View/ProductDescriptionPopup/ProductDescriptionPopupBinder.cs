using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Services.Pause;
using TMPro;
using UI.MVVM.Base;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Zenject;

namespace UI.MVVM.View.ProductDescriptionPopup
{
    public class ProductDescriptionPopupBinder : PopupBinder<ProductDescriptionPopupViewModel>
    {
        [Header("Descriptions")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _howToUseText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [Header("Flickering")]
        [SerializeField] private Image[] _segments;
        [SerializeField] private FlickerParams _params;

        [Header("Animations")]
        [SerializeField] private Transform _gifContainer;
        [SerializeField] private Transform _discriptionContainer;
        [SerializeField] private Transform _gifLinesTransform;
        [SerializeField] private FramePiece[] _framePieces;

        [SerializeField] private VideoPlayer _videoPlayer;

        private UIFlicker _uiFlicker;

        private IPauseService _pauseService;
        private CancellationTokenSource _cancellationTokenSource;
        private Tween _gifRotateTween;
        private Tween _discriptionRotateTween;
        private Tween _scaleLines;
        private Sequence _framePiecesSequence;


        [Inject]
        public void Construct(IPauseService pauseService) =>
            _pauseService = pauseService;

        protected override void Awake()
        {
            base.Awake();

            _uiFlicker = new UIFlicker();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        protected override void Start()
        {
            base.Start();

            _pauseService.StartPause();

            StartFlickAsync().Forget();
            Rotate();
            ScaleGifLines();
            MoveFramePiecesAsync().Forget();
        }

        protected override void OnDestroy()
        {
            Clear();

            base.OnDestroy();

            _pauseService.StopPause();
        }

        protected override void OnBind(ProductDescriptionPopupViewModel viewModel)
        {
            base.OnBind(viewModel);

            _titleText.text = viewModel.Description.TitleText;
            _howToUseText.text = viewModel.Description.HowToUseText;
            _descriptionText.text = viewModel.Description.DescriptionText;
            _videoPlayer.clip = viewModel.Description.VideoClip;
        }

        private async UniTask StartFlickAsync() =>
            await _uiFlicker.FlickerFor(_segments, _params, 0.2f, _cancellationTokenSource.Token);

        private void Rotate()
        {
            _gifRotateTween = _gifContainer.DORotate(Vector3.zero, 0.2f).SetUpdate(true);
            _discriptionRotateTween = _discriptionContainer.DORotate(Vector3.zero, 0.5f).SetUpdate(true);
        }

        private void ScaleGifLines()
        {
            _gifLinesTransform.localScale = Vector3.zero;
            _scaleLines = _gifLinesTransform.DOScale(Vector3.one, 2).SetUpdate(true).SetDelay(0.3f);
        }

        private async UniTask MoveFramePiecesAsync()
        {
            List<UniTask> tasks = new List<UniTask>();

            foreach (FramePiece framePiece in _framePieces)
            {
                float posX = framePiece.ParentTransform.localPosition.x;
                framePiece.ParentTransform.localPosition = new Vector3(0, framePiece.ParentTransform.localPosition.y,
                    framePiece.ParentTransform.localPosition.z);

                Tween tween = framePiece.ParentTransform
                    .DOLocalMoveX(posX, 0.5f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy).SetDelay(0.3f)
                    .OnComplete(() =>
                    {
                        framePiece.MaskTransform.DOLocalMoveY(0, 0.5f)
                            .SetUpdate(true)
                            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
                    });

                tasks.Add(tween.AsyncWaitForCompletion().AsUniTask());
            }

            await UniTask.WhenAll(tasks);
        }

        private void Clear()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _gifRotateTween?.Kill();
            _discriptionRotateTween?.Kill();
            _scaleLines?.Kill();
        }
    }

    [Serializable]
    public class FramePiece
    {
        public RectTransform ParentTransform;
        public RectTransform MaskTransform;
    }
}