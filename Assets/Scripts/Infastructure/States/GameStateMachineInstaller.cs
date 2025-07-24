using Zenject;

namespace Infastructure.States
{
    public class GameStateMachineInstaller : Installer<GameStateMachineInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindFactory<IStateMachine, BootstrapState, BootstrapState.Factory>();
            Container.BindFactory<IStateMachine, LoadProgressState, LoadProgressState.Factory>();
            Container.BindFactory<IStateMachine, LoadLevelState, LoadLevelState.Factory>();

            Container.BindFactory<MainMenuState, MainMenuState.Factory>();

            Container.Bind<IStateMachine>().To<StateMachine>().AsSingle();
        }
    }
}