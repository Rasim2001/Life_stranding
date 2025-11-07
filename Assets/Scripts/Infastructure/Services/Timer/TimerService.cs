using UnityEngine;
using Zenject;

namespace Infastructure.Services.Timer
{
    public class TimerService : ITimerService, ITickable
    {
        private float _timer;
        private bool _isStarting;

        public void StartTimer() =>
            _isStarting = true;

        public string GetTravelledTime()
        {
            int totalSeconds = Mathf.FloorToInt(_timer);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            string formatted = $"<font-weight=500>{minutes:00}:{seconds:00}<size=15>мин</size></font-weight>";

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