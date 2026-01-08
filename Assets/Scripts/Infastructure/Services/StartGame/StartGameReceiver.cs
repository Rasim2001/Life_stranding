using System;

namespace Infastructure.Services.StartGame
{
    public class StartGameReceiver : IStartGameReceiver
    {
        public event Action OnStartGameHappened;

        public void StartGame() => 
            OnStartGameHappened?.Invoke();
    }
}