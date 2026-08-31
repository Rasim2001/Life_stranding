using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Common;
using Infastructure.Services.Restart;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.World;
using Infastructure.World;
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
        private readonly ISegmentLoadingDirector _segmentLoadingDirector;

        public LoadLevelState(IStateMachine stateMachine, ISceneLoader sceneLoader,
            IStaticDataService staticDataService, ICurtainRoot curtainRoot, IRestartService restartService,
            ISegmentLoadingDirector segmentLoadingDirector)
        {
            _stateMachine = stateMachine;
            _sceneLoader = sceneLoader;
            _staticDataService = staticDataService;
            _curtainRoot = curtainRoot;
            _restartService = restartService;
            _segmentLoadingDirector = segmentLoadingDirector;
        }

        private TowerCatalog Catalog
        {
            get
            {
                TowerCatalog catalog = _staticDataService.GameStaticData.TowerCatalog;

                if (catalog == null)
                    throw new InvalidOperationException("GameStaticData.TowerCatalog is not assigned.");

                if (catalog.EntryScene == null || !catalog.EntryScene.IsValid)
                    throw new InvalidOperationException("TowerCatalog.EntryScene is not set.");

                if (catalog.AtmosphereScene == null || !catalog.AtmosphereScene.IsValid)
                    throw new InvalidOperationException("TowerCatalog.AtmosphereScene is not set.");

                return catalog;
            }
        }

        public void Enter()
        {
            _curtainRoot.Show();
            _sceneLoader.Load(Catalog.AtmosphereScene.SceneName, OnAtmosphereLoaded);
        }

        private void OnAtmosphereLoaded() =>
            LoadEntryAndSegmentsAsync().Forget();

        private async UniTaskVoid LoadEntryAndSegmentsAsync()
        {
            await _sceneLoader.LoadAdditiveAsync(Catalog.EntryScene.SceneName);
            await _segmentLoadingDirector.LoadInitialAsync();

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