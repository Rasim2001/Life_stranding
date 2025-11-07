using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Services.Pause;
using Infastructure.Services.SpiderTrack;
using Infastructure.States;
using SpiderController.StateMachine;
using TMPro;
using UI.MVVM.Base;
using UnityEngine;
using Zenject;

namespace UI.MVVM.View.DefeatPopup
{
    public class DefeatPopupBinder : PopupBinder<DefeatPopupViewModel>
    {
        private static readonly int StartTrigger = Animator.StringToHash("StartTrigger");

        [SerializeField] private TextMeshProUGUI _distanceToGoalText;
        [SerializeField] private Transform _container;
        [SerializeField] private FramePiecesUI _framePiecesUI;
        [SerializeField] private Animator _flowerAnimator;

        private IPauseService _pauseService;
        private IStateMachine _stateMachine;
        private ISpiderTrackService _spiderTrackService;

        private Tween _containerRotateTween;

        [Inject]
        public void Construct(IPauseService pauseService, IStateMachine stateMachine,
            ISpiderTrackService spiderTrackService)
        {
            _spiderTrackService = spiderTrackService;
            _stateMachine = stateMachine;
            _pauseService = pauseService;
        }


        protected override void Start()
        {
            base.Start();

            _pauseService.StartPause();

            StartFlowerAnimation().Forget();
            _framePiecesUI.MoveFramePiecesAsync().Forget();
            _containerRotateTween = _container.DORotate(Vector3.zero, 0.2f).SetUpdate(true);

            _distanceToGoalText.text = _spiderTrackService.GetDistanceToGoal();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _containerRotateTween?.Kill();
            _pauseService.StopPause();
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