using UnityEngine;

namespace Infastructure.Services.Pause
{
    public class PauseService : IPauseService
    {
        public bool IsPaused => Time.timeScale == 0;

        public void StartPause() =>
            Time.timeScale = 0;

        public void StopPause() =>
            Time.timeScale = 1;
    }
}