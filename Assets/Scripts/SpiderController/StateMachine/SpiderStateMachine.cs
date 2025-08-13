using System.Collections.Generic;
using System.Linq;
using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
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
            StateMachineData stateMachineData,
            IInputService inputService,
            IStaticDataService staticDataService,
            LegDataStruct[] legs,
            Flower flower)
        {
            _states = new List<ISpiderState>()
            {
                new IdlingState(this, inputService, staticDataService, spider, stateMachineData, legs, flower),
                new RunningState(this, inputService, staticDataService, spider, stateMachineData, legs, flower),
                new FastRunningState(this, inputService, staticDataService, spider, stateMachineData, legs, flower),
                new JumpingState(this, inputService, staticDataService, spider, stateMachineData, legs, flower),
                new FallingState(this, inputService, staticDataService, spider, stateMachineData, legs, flower),
                new FallingWithoutEnergyState(this, inputService, staticDataService, spider, stateMachineData, legs,
                    flower),
                new JerkState(this, inputService, staticDataService, spider, stateMachineData, legs, flower),
                new SlowdownState(this, inputService, staticDataService, spider, stateMachineData, legs, flower)
            };

            _currentState = _states[0];
            _currentState.Enter();
        }

        public void SwitchState<T>() where T : ISpiderState
        {
            ISpiderState newState = _states.FirstOrDefault(state => state is T);

            _currentState.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        public void HandleInput() => _currentState.HandleInput();

        public void Update() => _currentState.Update();

        public void FixedUpdate() => _currentState.FixedUpdate();

        public void LateUpdate() => _currentState.LateUpdate();
    }
}