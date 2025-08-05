using Infastructure.Common;
using Zenject;

namespace Infastructure.States
{
    public class MainMenuState : IState
    {
        private readonly ISceneLoader _sceneLoader;

        public MainMenuState(ISceneLoader sceneLoader) =>
            _sceneLoader = sceneLoader;

        public void Enter() =>
            _sceneLoader.Load(AssetsPath.MainMenuScene, OnLoaded);

        public void Exit()
        {
        }

        private void OnLoaded()
        {
        }

        public class Factory : PlaceholderFactory<MainMenuState>
        {
        }
    }
}