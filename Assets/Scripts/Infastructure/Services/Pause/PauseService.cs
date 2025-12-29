using System.Collections.Generic;
using UnityEngine;

namespace Infastructure.Services.Pause
{
    public class PauseService : IPauseService
    {
        private readonly List<string> _active = new();
        public bool IsPaused => _active.Count > 0;

        public void StartPause(string reason)
        {
            if (_active.Count == 0)
                Time.timeScale = 0;

            if (!_active.Contains(reason))
                _active.Add(reason);
        }


        public void StopPause(string reason)
        {
            if (_active.Contains(reason))
                _active.Remove(reason);

            if (_active.Count == 0)
                Time.timeScale = 1;
        }
    }
}