using System.Collections.Generic;
using System.Linq;
using _2;
using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States;
using SpiderController.StateMachine.States.Airborn;
using SpiderController.StateMachine.States.Ground;
using UnityEngine;

namespace SpiderController.StateMachine
{
    public class SpiderStateMachine : ISpiderStateMachine
    {
        private readonly List<ISpiderState> _states;

        private ISpiderState _currentState;

        public SpiderStateMachine(
            Spider spider,
            IInputService inputService,
            IStaticDataService staticDataService,
            LegDataStruct[] legs)
        {
            StateMachineData stateMachineData = new StateMachineData();

            _states = new List<ISpiderState>()
            {
                new IdlingState(this, inputService, staticDataService, spider, stateMachineData, legs),
                new RunningState(this, inputService, staticDataService, spider, stateMachineData, legs),
                new FastRunningState(this, inputService, staticDataService, spider, stateMachineData, legs),
                new JumpingState(this, inputService, staticDataService, spider, stateMachineData, legs),
                new FallingState(this, inputService, staticDataService, spider, stateMachineData, legs),
                new FallingWithoutEnergyState(this, inputService, staticDataService, spider, stateMachineData, legs)
            };

            _currentState = _states[0];
            _currentState.Enter();
        }

        public void SwitchState<T>() where T : ISpiderState
        {
            ISpiderState newState = _states.FirstOrDefault(state => state is T);

            //Debug.Log($"OldState : {_currentState.GetType().Name} and newState : {newState.GetType().Name}");

            _currentState.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        public void HandleInput() => _currentState.HandleInput();

        public void Update() => _currentState.Update();

        public void FixedUpdate() => _currentState.FixedUpdate();
    }
}