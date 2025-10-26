using Infastructure.Services.Window;
using UI.MVVM.Base;

namespace UI.MVVM.View.StartSplashScreen
{
    public class StartSplashScreenViewModel : WindowViewModel
    {
        public override string Id => "StartSplashScreen";

        private readonly IWindowService _windowService;

        public StartSplashScreenViewModel(IWindowService windowService) =>
            _windowService = windowService;

        public void RequestOpenPopupSettings() =>
            _windowService.OpenSettingsPopup();
    }
}