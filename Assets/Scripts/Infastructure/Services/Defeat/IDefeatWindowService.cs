using System;

namespace Infastructure.Services.Defeat
{
    public interface IDefeatWindowService
    {
        bool IsDefeated { get; }
        void OpenDefeatWindow();
        event Action OnDefeatHappened;
    }
}