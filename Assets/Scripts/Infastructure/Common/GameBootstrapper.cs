using Infastructure.States;
using UnityEngine;
using Zenject;

namespace Infastructure.Common
{
    public class GameBootstrapper : MonoBehaviour
    {
        private IStateMachine _stateMachine;

        [Inject]
        private void Construct(IStateMachine stateMachine) =>
            _stateMachine = stateMachine;

        private void Start()
        {
            _stateMachine.Enter<BootstrapState>();

            DontDestroyOnLoad(this);
        }

        public class Factory : PlaceholderFactory<GameBootstrapper>
        {
        }
    }
}