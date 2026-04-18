using System;

namespace Infastructure.Services.Pause
{
    public interface IPauseService
    {
        void StartPause(string reason);
        void StopPause(string reason);
        bool IsPaused { get; }
        event Action OnPauseChanged;
    }
}