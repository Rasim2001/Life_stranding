using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Common;
using Infastructure.Services.CutScene;
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
        private readonly ICutSceneService _cutSceneService;

        public LoadLevelState(IStateMachine stateMachine, ISceneLoader sceneLoader,
            IStaticDataService staticDataService, ICurtainRoot curtainRoot, IRestartService restartService,
            ICutSceneService cutSceneService)
        {
            _stateMachine = stateMachine;
            _sceneLoader = sceneLoader;
            _staticDataService = staticDataService;
            _curtainRoot = curtainRoot;
            _restartService = restartService;
            _cutSceneService = cutSceneService;
        }

        public void Enter()
        {
            _curtainRoot.Show();
            _sceneLoader.Load(_staticDataService.GameStaticData.LoadScene, OnLoaded);
        }

        private void OnLoaded() =>
            _sceneLoader.LoadAllScenes(_staticDataService.GameStaticData.AdditiveScenes, OnAdditiveSceneLoaded);

        private void OnAdditiveSceneLoaded()
        {
            if (_restartService.IsRestarting)
            {
                _cutSceneService.Skip();
                _restartService.Clear();

                HideCurtainAsync().Forget();
            }
            else
                _curtainRoot.Hide();
        }

        public void Exit()
        {
        }

        private async UniTask HideCurtainAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1));

            _curtainRoot.Hide();
        }


        public class Factory : PlaceholderFactory<IStateMachine, LoadLevelState>
        {
        }
    }
}