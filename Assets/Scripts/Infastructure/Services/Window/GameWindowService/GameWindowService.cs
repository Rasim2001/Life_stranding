using Infastructure.Factories.GameFactories;
using Infastructure.StaticData.Windows;

namespace Infastructure.Services.Window.GameWindowService
{
    public class GameWindowService : IGameWindowService
    {
        private readonly IGameUIFactory _gameUIFactory;

        public GameWindowService(IGameUIFactory gameUIFactory) =>
            _gameUIFactory = gameUIFactory;

        public void Open(WindowId windowId)
        {
            switch (windowId)
            {
                /*
                case WindowId.WinWindow:
                    _gameUIFactory.CreateWinWindow(WindowId.WinWindow);
                    break;
                    */

                /*
                case WindowId.DefeatWindow:
                    _gameUIFactory.CreateDefeatWindow(WindowId.DefeatWindow);
                    break;
            */
            }
        }
    }
}