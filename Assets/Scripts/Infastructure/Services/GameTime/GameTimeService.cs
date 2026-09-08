using System;
using System.Collections.Generic;
using Infastructure.Services.Pause;
using Infastructure.Services.SlowTime;
using Infastructure.StaticData.SlowTime;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;

namespace Infastructure.Services.GameTime
{
    public class GameTimeService : IPauseService, ISlowTimeRunner, ITickable
    {
        private const float NormalTimeScale = 1f;

        private readonly IStaticDataService _staticDataService;
        private readonly List<string> _pauseReasons = new();
        private readonly float _initialFixedDeltaTime;
        private readonly float _initialMaximumDeltaTime;

        private bool _slowDownActive;
        private float _slowDownElapsed;

        public event Action OnPauseChanged;
        public bool IsPaused => _pauseReasons.Count > 0;

        private SlowTimeStaticData Data => _staticDataService.SlowTimeStaticData;

        public GameTimeService(IStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;

            _initialFixedDeltaTime = Time.fixedDeltaTime;
            _initialMaximumDeltaTime = Time.maximumDeltaTime;
        }

        public void StartPause(string reason)
        {
            if (!_pauseReasons.Contains(reason))
                _pauseReasons.Add(reason);

            Apply();

            OnPauseChanged?.Invoke();
        }

        public void StopPause(string reason)
        {
            if (_pauseReasons.Contains(reason))
                _pauseReasons.Remove(reason);

            Apply();

            OnPauseChanged?.Invoke();
        }

        public void SlowDown()
        {
            _slowDownActive = true;
            _slowDownElapsed = 0f;

            Apply();
        }

        public void StopSlowDown()
        {
            _slowDownActive = false;

            Apply();
        }

        public void Tick()
        {
            if (_slowDownActive && !IsPaused)
            {
                _slowDownElapsed += Time.unscaledDeltaTime;

                if (_slowDownElapsed >= Data.Duration)
                    _slowDownActive = false;
            }

            Apply();
        }

        private void Apply()
        {
            float scale = EvaluateTimeScale();

            Time.timeScale = scale;

            if (scale > 0f)
            {
                Time.fixedDeltaTime = _initialFixedDeltaTime * scale;
                Time.maximumDeltaTime = _initialMaximumDeltaTime * scale;
            }
        }

        private float EvaluateTimeScale()
        {
            if (IsPaused)
                return 0f;

            if (!_slowDownActive)
                return NormalTimeScale;

            float lerpDuration = Data.LerpDuration;
            float t = lerpDuration > 0f ? Mathf.Clamp01(_slowDownElapsed / lerpDuration) : 1f;

            return Mathf.Lerp(NormalTimeScale, Data.TargetTimeScale, Data.EnterCurve.Evaluate(t));
        }
    }
}
