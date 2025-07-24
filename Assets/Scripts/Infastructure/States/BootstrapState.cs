using Infastructure.StaticData.StaticDataService;
using Zenject;

namespace Infastructure.States
{
    public class BootstrapState : IState
    {
        private readonly IStateMachine _stateMachine;
        private readonly IStaticDataService _staticDataService;

        public BootstrapState(IStateMachine stateMachine, IStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            InitServices();

            _stateMachine.Enter<LoadLevelState>();
        }

        private void InitServices() =>
            _staticDataService.LoadStaticData();

        public void Exit()
        {
        }

        public class Factory : PlaceholderFactory<IStateMachine, BootstrapState>
        {
        }
    }
}