using Infastructure.Services.PlayerProgressService;
using Infastructure.Services.SaveLoadService;
using UnityEngine;
using Zenject;

namespace Infastructure.States
{
    public class LoadProgressState : IState
    {
        private readonly IStateMachine _stateMachine;
        private readonly IPersistentProgressService _progressService;
        private readonly ISaveLoadService _saveLoadService;

        public LoadProgressState(
            IStateMachine stateMachine,
            IPersistentProgressService progressService,
            ISaveLoadService saveLoadService)
        {
            _stateMachine = stateMachine;
            _progressService = progressService;
            _saveLoadService = saveLoadService;
        }

        public void Enter()
        {
            LoadProgressOrInitNew();

            _stateMachine.Enter<LoadLevelState>();
        }

        public void Exit()
        {
        }

        private void LoadProgressOrInitNew() =>
            _progressService.PlayerProgress = _saveLoadService.LoadPlayerProgress();


        public class Factory : PlaceholderFactory<IStateMachine, LoadProgressState>
        {
        }
    }
}