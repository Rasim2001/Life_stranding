using DG.Tweening;
using Infastructure.Common;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;

namespace Infastructure.States
{
    public class LoadLevelState : IState
    {
        private readonly IStateMachine _stateMachine;
        private readonly ISceneLoader _sceneLoader;
        private readonly IStaticDataService _staticDataService;

        public LoadLevelState(IStateMachine stateMachine, ISceneLoader sceneLoader,
            IStaticDataService staticDataService)
        {
            _stateMachine = stateMachine;
            _sceneLoader = sceneLoader;
            _staticDataService = staticDataService;
        }

        public void Enter() =>
            _sceneLoader.Load(_staticDataService.GameStaticData.LoadScene, OnLoaded);

        private void OnLoaded() =>
            _sceneLoader.LoadAllScenes(_staticDataService.GameStaticData.AdditiveScenes);

        public void Exit()
        {
        }


        public class Factory : PlaceholderFactory<IStateMachine, LoadLevelState>
        {
        }
    }
}