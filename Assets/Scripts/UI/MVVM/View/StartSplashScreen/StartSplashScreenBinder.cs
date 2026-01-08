using DG.Tweening;
using Infastructure.Common;
using Infastructure.Services.SaveLoadService;
using Infastructure.Services.StartGame;
using Infastructure.Services.Window;
using UI.Curtain;
using UI.MVVM.Base;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace UI.MVVM.View.StartSplashScreen
{
    public class StartSplashScreenBinder : WindowBinder<StartSplashScreenViewModel>
    {
        private static readonly int GrowTrigger = Animator.StringToHash("GrowTrigger");

        [SerializeField] private Button _startGameBtn;
        [SerializeField] private Button _continueGameBtn;
        [SerializeField] private Button _settingsPopupBtn;
        [SerializeField] private Button _exitBtn;
        [SerializeField] private Animator _flowerAnimator;
        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField] private GameObject _menuContainer;

        private Tween _canvasTween;

        private IEventSystemSelector _eventSystemSelector;
        private IStartGameReceiver _startGameReceiver;
        private ICurtainRoot _curtain;
        private ISaveLoadService _saveLoadService;

        [Inject]
        public void Construct(IEventSystemSelector eventSystemSelector, IStartGameReceiver startGameReceiver,
            ICurtainRoot curtain, ISaveLoadService saveLoadService)
        {
            _saveLoadService = saveLoadService;
            _curtain = curtain;
            _startGameReceiver = startGameReceiver;
            _eventSystemSelector = eventSystemSelector;
        }


        private void Start()
        {
            _eventSystemSelector.SelectButton(_startGameBtn.gameObject);

            _startGameBtn.onClick.AddListener(NewGame);
            _continueGameBtn.onClick.AddListener(ContinueGame);
            _settingsPopupBtn.onClick.AddListener(OpenSettingsPopup);
            _exitBtn.onClick.AddListener(Exit);
        }

        private void OnDestroy()
        {
            _startGameBtn.onClick.RemoveListener(NewGame);
            _continueGameBtn.onClick.RemoveListener(ContinueGame);
            _settingsPopupBtn.onClick.RemoveListener(OpenSettingsPopup);
            _exitBtn.onClick.RemoveListener(Exit);

            _canvasTween?.Kill();
        }

        private void OpenSettingsPopup() =>
            ViewModel.RequestOpenSettingScreen();

        private void NewGame()
        {
            _saveLoadService.SetNewProgress();

            InitGame();
        }

        private void ContinueGame()
        {
            _saveLoadService.SetContinueProgress();

            InitGame();
        }

        private void InitGame()
        {
            _curtain.ShowAndHide();

            _menuContainer.SetActive(false);
            _flowerAnimator.SetTrigger(GrowTrigger);

            _canvasTween = DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 0, 1)
                .SetDelay(2)
                .OnComplete(() =>
                {
                    Instantiate(Resources.Load<GameObject>(AssetsPath.WaterFallsPath));

                    _startGameReceiver.StartGame();
                    ViewModel.RequestClose();
                });
        }


        private void Exit() =>
            Application.Quit();
    }
}