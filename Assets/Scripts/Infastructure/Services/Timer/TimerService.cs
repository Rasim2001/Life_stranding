using Infastructure.Localization;
using Infastructure.StaticData.StaticDataService;
using Localization;
using UnityEngine;
using Zenject;

namespace Infastructure.Services.Timer
{
    public class TimerService : ITimerService, ITickable
    {
        private readonly ILocalizationService _localizationService;
        private readonly IStaticDataService _staticDataService;

        private float _timer;
        private bool _isStarting;

        public TimerService(ILocalizationService localizationService, IStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;
            _localizationService = localizationService;
        }

        public void StartTimer() =>
            _isStarting = true;

        public string GetTravelledTime()
        {
            LocalizationText localizationText =
                _staticDataService.WindowsLocalizationStaticData.Texts[TextStaticId.Minute_WinPopup];
            string minuteText = localizationText.Get(_localizationService.CurrentLanguage);

            int totalSeconds = Mathf.FloorToInt(_timer);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            string formatted = $"<font-weight=500>{minutes:00}:{seconds:00}<size=15>{minuteText}</size></font-weight>";

            return formatted;
        }

        public void Tick()
        {
            if (!_isStarting)
                return;

            _timer += Time.deltaTime;
        }
    }
}