using _2;
using CameraFollow;
using Infastructure.Common;
using SpiderController;
using UnityEngine;
using Zenject;

namespace Infastructure.Factories.GameFactories
{
    public class GameFactory : IGameFactory
    {
        private readonly DiContainer _diContainer;

        public GameFactory(DiContainer diContainer) =>
            _diContainer = diContainer;

        public GameObject CreateSpider()
        {
            Spider spider = _diContainer.InstantiatePrefabResourceForComponent<Spider>(AssetsPath.SpiderPath);
            spider.Initialize();

            return spider.gameObject;
        }

        public void CreateCameraSystem(Transform spiderTransform)
        {
            CameraSystem cameraSystem =
                _diContainer.InstantiatePrefabResourceForComponent<CameraSystem>(AssetsPath.CameraSystemPath);
            cameraSystem.Initialize(spiderTransform);
        }

        public void CreateFlowerFollower(Transform spiderTransform)
        {
            FlowerFollower flowerFollower =
                _diContainer.InstantiatePrefabResourceForComponent<FlowerFollower>(AssetsPath.FlowerFollower);

            flowerFollower.SetTarget(spiderTransform);
        }
    }
}