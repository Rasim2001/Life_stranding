using Infastructure.Services.Ability;
using Infastructure.Services.Pause;
using Infastructure.Services.PauseWindow;
using Infastructure.Services.Restart;
using Infastructure.Services.SlowTime;
using Infastructure.States;
using UI.Curtain;
using UI.MVVM.Base;
using UI.MVVM.View.Root;
using UI.MVVM.View.SettingsPopup;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace UI.MVVM.View.PausePopup
{
    public class PausePopupBinder : PopupBinder<PausePopupViewModel>
    {
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _gotoMenu;
        [SerializeField] private Button _exit;

        private IPauseService _pauseService;
        private IPauseWindowService _pauseWindowService;
        private IRestartService _restartService;
        private IAbilityService _abilityService;
        private IStateMachine _stateMachine;
        private ICurtainRoot _curtainRoot;
        private ISlowTimeRunner _slowTimeRunner;
        private UIGameplayRootViewModel _gameplayRootViewModel;

        [Inject]
        public void Construct(IPauseService pauseService, IPauseWindowService pauseWindowService,
            IRestartService restartService, IAbilityService abilityService, IStateMachine stateMachine,
            ICurtainRoot curtainRoot, ISlowTimeRunner slowTimeRunner, UIGameplayRootViewModel gameplayRootViewModel)
        {
            _gameplayRootViewModel = gameplayRootViewModel;
            _slowTimeRunner = slowTimeRunner;
            _curtainRoot = curtainRoot;
            _stateMachine = stateMachine;
            _abilityService = abilityService;
            _restartService = restartService;
            _pauseWindowService = pauseWindowService;
            _pauseService = pauseService;
        }

        protected override void Start()
        {
            base.Start();

            _restartButton.onClick.AddListener(Restart);
            _gotoMenu.onClick.AddListener(GoToMenu);
            _exit.onClick.AddListener(Exit);

            if (_slowTimeRunner.IsRunning())
                _slowTimeRunner.StopSlowDown();

            _pauseService.StartPause(gameObject.name);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _restartButton.onClick.RemoveListener(Restart);
            _gotoMenu.onClick.RemoveListener(GoToMenu);
            _exit.onClick.RemoveListener(Exit);

            _pauseService.StopPause(gameObject.name);

            if (_slowTimeRunner.IsRunning())
                _slowTimeRunner.SlowDown();
        }


        protected override void OnCloseButtonClick()
        {
            base.OnCloseButtonClick();

            _pauseWindowService.IsOpened = false;
        }


        private void Restart()
        {
            _curtainRoot.Show();

            _gameplayRootViewModel.Clear();
            _restartService.Restart(_abilityService.GetAllExploredAbilities());

            _stateMachine.Enter<LoadProgressState>();
        }

        private void GoToMenu()
        {
            _gameplayRootViewModel.Clear();

            _stateMachine.Enter<ExitGameLoopState>();
        }

        private void Exit() =>
            Application.Quit();
    }
}