using UnityEngine;

namespace Infastructure.Factories.GameFactories
{
    public interface IGameFactory
    {
        GameObject CreateSpider();
        GameObject CreateCameraSystem();
    }
}