using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Services.Pause;
using Infastructure.States;
using UI.MVVM.Base;
using UI.MVVM.View.TaskPopup;
using UnityEngine;
using Zenject;

namespace UI.MVVM.View.DefeatPopup
{
    public class DefeatPopupBinder : PopupBinder<DefeatPopupViewModel>
    {
        [SerializeField] private Transform _container;
        [SerializeField] private FramePiecesUI _framePiecesUI;

        private IPauseService _pauseService;
        private IStateMachine _stateMachine;

        private Tween _containerRotateTween;

        [Inject]
        public void Construct(IPauseService pauseService, IStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
            _pauseService = pauseService;
        }


        protected override void Start()
        {
            base.Start();

            _pauseService.StartPause();

            _framePiecesUI.MoveFramePiecesAsync().Forget();
            _containerRotateTween = _container.DORotate(Vector3.zero, 0.2f).SetUpdate(true);
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
    }
}