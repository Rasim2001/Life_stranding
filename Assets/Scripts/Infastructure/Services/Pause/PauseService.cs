using UnityEngine;

namespace Infastructure.Services.Pause
{
    public class PauseService : IPauseService
    {
        public void StartPause() =>
            Time.timeScale = 0;

        public void StopPause() =>
            Time.timeScale = 1;
    }
}