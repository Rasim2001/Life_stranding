using UnityEngine;
using Zenject;

namespace Infastructure.Factories.GameFactories
{
    public class GameUIFactory : IGameUIFactory
    {
        private readonly DiContainer _diContainer;

        private IGameUIFactory _iuiFactory;
        private GameObject _uiRoot;

        public GameUIFactory(DiContainer diContainer) =>
            _diContainer = diContainer;
    }
}