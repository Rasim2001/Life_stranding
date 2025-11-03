using DG.Tweening;
using Infastructure.Common;
using UI.Curtain;
using Zenject;

namespace Infastructure.States
{
    public class ExitGameLoopState : IState
    {
        private const string ExitGameLoop = "ExitGameLoop";

        private readonly IStateMachine _stateMachine;
        private readonly ISceneLoader _sceneLoader;
        private readonly ICurtainRoot _curtainRoot;

        public ExitGameLoopState(IStateMachine stateMachine, ISceneLoader sceneLoader, ICurtainRoot curtainRoot)
        {
            _stateMachine = stateMachine;
            _sceneLoader = sceneLoader;
            _curtainRoot = curtainRoot;
        }

        public void Enter()
        {
            DOTween.KillAll();

            _curtainRoot.Show();
            _sceneLoader.Load(ExitGameLoop, OnLoaded);
        }

        private void OnLoaded() =>
            _stateMachine.Enter<LoadLevelState>();

        public void Exit()
        {
        }

        public class Factory : PlaceholderFactory<IStateMachine, ExitGameLoopState>
        {
        }
    }
}