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
        [SerializeField] private Button _startGameBtn;
        [SerializeField] private Button _settingsPopupBtn;

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
        }

        private void OnDestroy()
        {
            _startGameBtn.onClick.RemoveListener(StartGame);
            _settingsPopupBtn.onClick.RemoveListener(OpenSettingsPopup);
        }

        private void OpenSettingsPopup() =>
            ViewModel.RequestOpenSettingScreen();

        private void StartGame()
        {
            _cutSceneService.IsActive = true;

            ViewModel.RequestClose();
        }
    }
}