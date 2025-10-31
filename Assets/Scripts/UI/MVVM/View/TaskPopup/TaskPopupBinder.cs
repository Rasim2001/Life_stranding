using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Services.Pause;
using UI.MVVM.Base;
using UI.MVVM.View.ProductDescriptionPopup;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace UI.MVVM.View.TaskPopup
{
    public class TaskPopupBinder : PopupBinder<TaskPopupViewModel>
    {
        [Header("Flickering")]
        [SerializeField] private Image[] _segments;
        [SerializeField] private FlickerParams _params;
        [SerializeField] private FramePiece[] _framePieces;
        [SerializeField] private Transform _container;


        private UIFlicker _uiFlicker;
        private CancellationTokenSource _cancellationTokenSource;

        private IPauseService _pauseService;
        private Tween _containerRotateTween;

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
            MoveFramePiecesAsync().Forget();
            Rotate();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Clear();

            _pauseService.StopPause();
        }

        private async UniTask StartFlickAsync() =>
            await _uiFlicker.FlickerFor(_segments, _params, 0.2f, _cancellationTokenSource.Token);

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

        private void Rotate() =>
            _containerRotateTween = _container.DORotate(Vector3.zero, 0.2f).SetUpdate(true);

        private void Clear()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _containerRotateTween?.Kill();
        }
    }
}