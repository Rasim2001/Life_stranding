using Infastructure.Factories.GameFactories;
using Infastructure.Services.Window.GameWindowService;
using Infastructure.States;
using UnityEngine;
using Zenject;

namespace Infastructure.CompositionRoot
{
    public class SceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Debug.Log("Scene Installer");
            BindBuildLevelState();

            BindWindowService();

            BindGameFactory();

            BindUIFactory();
        }


        private void BindBuildLevelState() =>
            Container.BindInterfacesAndSelfTo<BuildLevelState>().AsSingle();

        private void BindUIFactory() =>
            Container.BindInterfacesAndSelfTo<GameUIFactory>().AsSingle();


        private void BindGameFactory() =>
            Container.BindInterfacesAndSelfTo<GameFactory>().AsSingle();

        private void BindWindowService() =>
            Container.BindInterfacesAndSelfTo<GameWindowService>().AsSingle();
    }
}