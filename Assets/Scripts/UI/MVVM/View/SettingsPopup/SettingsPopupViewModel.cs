using Infastructure.Services.Window;
using UI.MVVM.Base;

namespace UI.MVVM.View.SettingsPopup
{
    public class SettingsPopupViewModel : WindowViewModel
    {
        public override string Id => "Settings";

        private readonly IWindowService _windowService;

        public SettingsPopupViewModel(IWindowService windowService) =>
            _windowService = windowService;
    }
}