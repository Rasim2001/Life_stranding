using Infastructure.Services.CutScene;
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

        [Inject]
        public void Construct(ICutSceneService cutSceneService) =>
            _cutSceneService = cutSceneService;

        private void Start()
        {
            _startGameBtn.onClick.AddListener(StartGame);
            _settingsPopupBtn.onClick.AddListener(OpenSettingsPopup);
        }

        private void OnDestroy()
        {
            _startGameBtn.onClick.RemoveListener(StartGame);
            _settingsPopupBtn.onClick.RemoveListener(OpenSettingsPopup);
        }

        private void OpenSettingsPopup() =>
            ViewModel.RequestOpenPopupSettings();

        private void StartGame()
        {
            _cutSceneService.IsActive = true;

            ViewModel.RequestClose();
        }
    }
}