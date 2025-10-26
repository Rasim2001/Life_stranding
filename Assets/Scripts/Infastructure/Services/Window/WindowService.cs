using UI.MVVM.View.Root;
using UI.MVVM.View.SettingsPopup;
using UI.MVVM.View.StartSplashScreen;

namespace Infastructure.Services.Window
{
    public class WindowService : IWindowService
    {
        private readonly UIGameplayRootViewModel _gamePlayViewModel;

        public WindowService(UIGameplayRootViewModel gamePlayViewModel) =>
            _gamePlayViewModel = gamePlayViewModel;

        public void OpenStartSplashScreen()
        {
            StartSplashScreenViewModel viewModel = new StartSplashScreenViewModel(this);

            _gamePlayViewModel.OpenScreen(viewModel);
        }

        public void OpenSettingsPopup()
        {
            SettingsPopupViewModel model = new SettingsPopupViewModel(this);

            _gamePlayViewModel.OpenPopup(model);
        }
    }
}