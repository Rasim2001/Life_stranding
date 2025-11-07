using System;
using Cysharp.Threading.Tasks;
using Infastructure.Services.Window;

namespace Infastructure.Services.Defeat
{
    public class DefeatWindowService : IDefeatWindowService
    {
        public event Action OnDefeatHappened;

        public bool IsDefeated
        {
            get => _isDefeated;
            set
            {
                if (value)
                    OnDefeatHappened?.Invoke();

                _isDefeated = value;
            }
        }

        private bool _isDefeated;

        private readonly IWindowService _windowService;

        public DefeatWindowService(IWindowService windowService) =>
            _windowService = windowService;

        public void OpenDefeatWindow()
        {
            IsDefeated = true;

            DefeatAsync().Forget();
        }

        private async UniTask DefeatAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(2));

            _windowService.OpenDefeatPopup();
        }
    }
}