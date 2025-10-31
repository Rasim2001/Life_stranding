using System.Threading;
using DG.Tweening;
using Infastructure.Services.CutScene;
using Infastructure.Services.Window;
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
        [SerializeField] private Button _settingsPopupBtn;
        [SerializeField] private Animator _flowerAnimator;
        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField] private GameObject _menuContainer;

        private Tween _canvasTween;

        private ICutSceneService _cutSceneService;
        private IEventSystemSelector _eventSystemSelector;

        [Inject]
        public void Construct(ICutSceneService cutSceneService, IEventSystemSelector eventSystemSelector)
        {
            _eventSystemSelector = eventSystemSelector;
            _cutSceneService = cutSceneService;
        }


        private void Start()
        {
            _eventSystemSelector.SelectButton(_startGameBtn.gameObject);

            _startGameBtn.onClick.AddListener(StartGame);
            _settingsPopupBtn.onClick.AddListener(OpenSettingsPopup);

            _cutSceneService.OnSkipHappened += Skip;
        }

        private void OnDestroy()
        {
            _startGameBtn.onClick.RemoveListener(StartGame);
            _settingsPopupBtn.onClick.RemoveListener(OpenSettingsPopup);

            _cutSceneService.OnSkipHappened -= Skip;
        }

        private void OpenSettingsPopup() =>
            ViewModel.RequestOpenSettingScreen();

        private void StartGame()
        {
            _cutSceneService.IsActive = true;

            _menuContainer.SetActive(false);
            _flowerAnimator.SetTrigger(GrowTrigger);

            _canvasTween = DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 0, 1)
                .SetDelay(2)
                .OnComplete(() => ViewModel.RequestClose());
        }


        private void Skip()
        {
            _canvasTween.Kill();

            ViewModel.RequestClose();
        }
    }
}