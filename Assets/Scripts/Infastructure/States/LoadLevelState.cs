using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Infastructure.States
{
    public class LoadLevelState : IState
    {
        private readonly IStateMachine _stateMachine;
        private readonly ISceneLoader _sceneLoader;

        public LoadLevelState(IStateMachine stateMachine, ISceneLoader sceneLoader)
        {
            _stateMachine = stateMachine;
            _sceneLoader = sceneLoader;
        }

        public void Enter() => 
            _sceneLoader.Load(AssetsPath.GameScene, OnLoaded);

        private void OnLoaded()
        {
        }

        public void Exit()
        {
        }


        public class Factory : PlaceholderFactory<IStateMachine, LoadLevelState>
        {
        }
    }
}