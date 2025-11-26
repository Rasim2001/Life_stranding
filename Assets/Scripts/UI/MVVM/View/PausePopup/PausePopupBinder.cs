using Infastructure.Services.Ability;
using Infastructure.Services.Pause;
using Infastructure.Services.PauseWindow;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerInput.InputSourceRealization;
using Infastructure.Services.Restart;
using Infastructure.States;
using UI.MVVM.Base;
using UI.MVVM.View.SettingsPopup;
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

        [SerializeField] private GameObject _gamepadContainer;
        [SerializeField] private GameObject _keyboardContainer;

        private IPauseService _pauseService;
        private IPauseWindowService _pauseWindowService;
        private IRestartService _restartService;
        private IAbilityService _abilityService;
        private IStateMachine _stateMachine;
        private IInputService _inputService;

        [Inject]
        public void Construct(IPauseService pauseService, IPauseWindowService pauseWindowService,
            IRestartService restartService, IAbilityService abilityService, IStateMachine stateMachine,
            IInputService inputService)
        {
            _inputService = inputService;
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

            _inputService.OnJoystickEnableHappend += JoystickEnabled;
            _inputService.OnJoystickDisableHappend += JoystickDisabled;

            bool isGamepadActive = _inputService.IsActiveSource<JoystickInputSource>();
            _gamepadContainer.SetActive(isGamepadActive);
            _keyboardContainer.SetActive(!isGamepadActive);

            _pauseService.StartPause();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _restartButton.onClick.RemoveListener(Restart);
            _gotoMenu.onClick.RemoveListener(GoToMenu);
            _exit.onClick.RemoveListener(Exit);

            _inputService.OnJoystickEnableHappend -= JoystickEnabled;
            _inputService.OnJoystickDisableHappend -= JoystickDisabled;

            _pauseService.StopPause();
        }


        protected override void OnCloseButtonClick()
        {
            base.OnCloseButtonClick();

            _pauseWindowService.IsOpened = false;
        }

        private void JoystickDisabled()
        {
            _gamepadContainer.SetActive(false);
            _keyboardContainer.SetActive(true);
        }

        private void JoystickEnabled(IInputSource obj)
        {
            _gamepadContainer.SetActive(true);
            _keyboardContainer.SetActive(false);
        }


        private void Restart()
        {
            _restartService.Restart(_abilityService.GetAllExploredAbilities());

            _stateMachine.Enter<ExitGameLoopState>();
        }

        private void GoToMenu() =>
            _stateMachine.Enter<ExitGameLoopState>();

        private void Exit() =>
            Application.Quit();
    }
}