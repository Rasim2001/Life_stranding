using System;

namespace Infastructure.Services.StartGame
{
    public interface IStartGameReceiver
    {
        event Action OnStartGameHappened;
        void StartGame();
    }
}