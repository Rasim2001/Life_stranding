using System;
using DG.Tweening;
using Infastructure.Factories.GameFactories;
using Infastructure.Services.PlayerProgressService;
using Infastructure.StaticData.StaticDataService;
using Zenject;

namespace Infastructure.States
{
    public class BuildLevelState : IInitializable, IDisposable
    {
        private readonly IGameFactory _gameFactory;
        private readonly IStaticDataService _staticData;
        private readonly IGameUIFactory _uiFactory;
        private readonly IPersistentProgressService _progressService;

        public BuildLevelState(
            IGameFactory gameFactory,
            IStaticDataService staticData,
            IGameUIFactory uiFactory,
            IPersistentProgressService progressService
        )
        {
            _gameFactory = gameFactory;
            _staticData = staticData;
            _uiFactory = uiFactory;
            _progressService = progressService;
        }

        public void Initialize() =>
            InitGameWorld();


        public void Dispose() =>
            DOTween.KillAll();


        private void InitGameWorld()
        {
            /*InitUIRoot();
            InitHud();
            InitTable();
            InitInput();*/
        }
    }
}