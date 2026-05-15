using Infastructure.Services.Window;
using UI.MVVM.Base;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace UI.MVVM.View.SettingsScreen
{
    public class SettingsScreenBinder : WindowBinder<SettingsScreenViewModel>
    {
        [SerializeField] private Button _openSplashScreenBtn;

        private IEventSystemSelector _eventSystemSelector;

        [Inject]
        public void Construct(IEventSystemSelector eventSystemSelector) =>
            _eventSystemSelector = eventSystemSelector;

        private void Start()
        {
            _eventSystemSelector.SelectButton(_openSplashScreenBtn.gameObject);

            _openSplashScreenBtn.onClick.AddListener(OpenSplashScreen);
        }

        protected void OnDestroy() => 
            _openSplashScreenBtn.onClick.RemoveListener(OpenSplashScreen);

        private void OpenSplashScreen() =>
            ViewModel.RequestOpenStartSplashScreen();
    }
}