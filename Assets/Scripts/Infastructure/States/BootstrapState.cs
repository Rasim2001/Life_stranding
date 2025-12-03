using Infastructure.Services.CursorVisible;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;

namespace Infastructure.States
{
    public class BootstrapState : IState
    {
        private readonly IStateMachine _stateMachine;
        private readonly IStaticDataService _staticDataService;
        private readonly ICursorVisibleService _cursorVisibleService;

        public BootstrapState(IStateMachine stateMachine, IStaticDataService staticDataService,
            ICursorVisibleService cursorVisibleService)
        {
            _staticDataService = staticDataService;
            _cursorVisibleService = cursorVisibleService;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            InitServices();

            _stateMachine.Enter<LoadLevelState>();
        }

        private void InitServices()
        {
            _cursorVisibleService.Initialize();

            _staticDataService.LoadStaticData();
        }

        public void Exit()
        {
        }

        public class Factory : PlaceholderFactory<IStateMachine, BootstrapState>
        {
        }
    }
}