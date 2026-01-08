using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Zenject;

namespace Infastructure.Services.PauseWindow
{
    public class PauseWindowService : IPauseWindowService, ITickable
    {
        private const string ID = "PausePopup";

        private readonly IInputService _inputService;
        private readonly IWindowService _windowService;

        public bool IsOpened { get; set; }

        public PauseWindowService(IInputService inputService, IWindowService windowService)
        {
            _inputService = inputService;
            _windowService = windowService;
        }

        public void Tick()
        {
            if (_inputService.PauseButtonPressed)
            {
                if (IsOpened == false && !_windowService.CanOpenWindow())
                    return;

                IsOpened = !IsOpened;

                if (IsOpened)
                    _windowService.OpenPausePopup();
                else
                    _windowService.ClosePopup(ID);
            }
        }
    }
}