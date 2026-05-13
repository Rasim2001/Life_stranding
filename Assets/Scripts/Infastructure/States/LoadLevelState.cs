using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Common;
using Infastructure.Services.Restart;
using Infastructure.StaticData.StaticDataService;
using UI.Curtain;
using UnityEngine;
using Zenject;

namespace Infastructure.States
{
    public class LoadLevelState : IState
    {
        private readonly IStateMachine _stateMachine;
        private readonly ISceneLoader _sceneLoader;
        private readonly IStaticDataService _staticDataService;
        private readonly ICurtainRoot _curtainRoot;
        private readonly IRestartService _restartService;

        public LoadLevelState(IStateMachine stateMachine, ISceneLoader sceneLoader,
            IStaticDataService staticDataService, ICurtainRoot curtainRoot, IRestartService restartService)
        {
            _stateMachine = stateMachine;
            _sceneLoader = sceneLoader;
            _staticDataService = staticDataService;
            _curtainRoot = curtainRoot;
            _restartService = restartService;
        }

        public void Enter()
        {
            //_curtainRoot.Show();
            _sceneLoader.Load(_staticDataService.GameStaticData.LoadScene, OnLoaded);
        }

        private void OnLoaded() =>
            _sceneLoader.LoadAllScenes(_staticDataService.GameStaticData.AdditiveScenes, OnAdditiveSceneLoaded);

        private void OnAdditiveSceneLoaded()
        {
            _curtainRoot.Hide();
            _restartService.Clear();
        }

        public void Exit()
        {
        }


        public class Factory : PlaceholderFactory<IStateMachine, LoadLevelState>
        {
        }
    }
}