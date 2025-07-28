using UnityEngine;
using Zenject;

namespace Infastructure.Factories.GameFactories
{
    public class GameFactory : IGameFactory
    {
        private readonly DiContainer _diContainer;

        private GameObject _hudObject;

        public GameFactory(DiContainer diContainer) =>
            _diContainer = diContainer;

        public GameObject CreateSpider() =>
            _diContainer.InstantiatePrefabResource(AssetsPath.SpiderPath);

        public GameObject CreateCameraSystem() =>
            _diContainer.InstantiatePrefabResource(AssetsPath.CameraSystemPath);
    }
}