using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Services.Pause;
using Infastructure.Services.Timer;
using Infastructure.States;
using TMPro;
using UI.MVVM.Base;
using UnityEngine;
using Zenject;

namespace UI.MVVM.View.WinPopup
{
    public class WinPopupBinder : PopupBinder<WinPopupViewModel>
    {
        private static readonly int StartTrigger = Animator.StringToHash("StartTrigger");

        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private FramePiecesUI _framePiecesUI;
        [SerializeField] private Animator _flowerAnimator;
        [SerializeField] private Transform _container;

        private IStateMachine _stateMachine;
        private IPauseService _pauseService;
        private ITimerService _timerService;

        private Tween _containerRotateTween;

        [Inject]
        public void Construct(IPauseService pauseService, IStateMachine stateMachine, ITimerService timerService)
        {
            _timerService = timerService;
            _stateMachine = stateMachine;
            _pauseService = pauseService;
        }

        protected override void Start()
        {
            base.Start();

            _pauseService.StartPause(gameObject.name);

            StartFlowerAnimation().Forget();
            _framePiecesUI.MoveFramePiecesAsync().Forget();
            _containerRotateTween = _container.DORotate(Vector3.zero, 0.2f).SetUpdate(true);

            _timerText.text = _timerService.GetTravelledTime();
        }

        protected void OnDestroy()
        {
            _containerRotateTween?.Kill();
            _pauseService.StopPause(gameObject.name);
        }

        protected override void OnCloseButtonClick()
        {
            base.OnCloseButtonClick();

            _stateMachine.Enter<ExitGameLoopState>();
        }

        private async UniTask StartFlowerAnimation()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.75f), ignoreTimeScale: true);

            _flowerAnimator.SetTrigger(StartTrigger);
        }
    }
}