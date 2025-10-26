using Infastructure.Services.Window;
using UI.MVVM.Base;

namespace UI.MVVM.View.SettingsScreen
{
    public class SettingsScreenViewModel : WindowViewModel
    {
        public override string Id => "SettingsScreen";

        private readonly IWindowService _windowService;

        public SettingsScreenViewModel(IWindowService windowService) =>
            _windowService = windowService;

        public void RequestOpenStartSplashScreen() =>
            _windowService.OpenStartSplashScreen();
    }
}