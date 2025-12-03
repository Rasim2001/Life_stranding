using System;
using Infastructure.Services.QuitApplication;
using Infastructure.States;
using UnityEngine;
using Zenject;

namespace Infastructure.Common
{
    public class GameBootstrapper : MonoBehaviour
    {
        private IStateMachine _stateMachine;
        private IQuitGameService _quitGameService;

        [Inject]
        private void Construct(IStateMachine stateMachine, IQuitGameService quitGameService)
        {
            _quitGameService = quitGameService;
            _stateMachine = stateMachine;
        }

        private void Start()
        {
            _stateMachine.Enter<BootstrapState>();

            DontDestroyOnLoad(this);
        }

        private void OnApplicationQuit() =>
            _quitGameService.QuitGame();

        public class Factory : PlaceholderFactory<GameBootstrapper>
        {
        }
    }
}