using System;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Infastructure.Services.Pause
{
    public class PauseService : IPauseService
    {
        private readonly List<string> _active = new();
        public event Action OnPauseChanged;
        public bool IsPaused => _active.Count > 0;

        public void StartPause(string reason)
        {
            if (_active.Count == 0)
            {
                MMTimeManager.Instance.UpdateTimescale = false;
                Time.timeScale = 0;
            }

            if (!_active.Contains(reason))
                _active.Add(reason);

            OnPauseChanged?.Invoke();
        }


        public void StopPause(string reason)
        {
            if (_active.Contains(reason))
                _active.Remove(reason);

            if (_active.Count == 0)
            {
                MMTimeManager.Instance.UpdateTimescale = true;
                Time.timeScale = 1;
            }

            OnPauseChanged?.Invoke();
        }
    }
}