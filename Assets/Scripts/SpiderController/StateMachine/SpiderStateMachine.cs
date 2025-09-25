using System.Collections.Generic;
using System.Linq;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
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

        public SpiderStateMachine(Spider spider,
            StateMachineData stateMachineData,
            IInputService inputService,
            IStaticDataService staticDataService,
            LegDataStruct[] legs,
            Flower flower,
            EnergySystem energySystem)
        {
            _states = new List<ISpiderState>()
            {
                new IdlingState(this, inputService, staticDataService, spider, stateMachineData, legs, flower,
                    energySystem),
                new RunningState(this, inputService, staticDataService, spider, stateMachineData, legs, flower,
                    energySystem),
                new FastRunningState(this, inputService, staticDataService, spider, stateMachineData, legs, flower,
                    energySystem),
                new JumpingState(this, inputService, staticDataService, spider, stateMachineData, legs, flower,
                    energySystem),
                new FallingState(this, inputService, staticDataService, spider, stateMachineData, legs, flower,
                    energySystem),
                new FallingWithoutEnergyState(this, inputService, staticDataService, spider, stateMachineData, legs,
                    flower, energySystem),
                new JerkState(this, inputService, staticDataService, spider, stateMachineData, legs, flower,
                    energySystem),
                new SlowdownState(this, inputService, staticDataService, spider, stateMachineData, legs, flower,
                    energySystem),
                new FallingWithControlState(this, inputService, staticDataService, spider, stateMachineData, legs,
                    flower, energySystem),
                new RecoveryState(this, inputService, staticDataService, spider, stateMachineData, legs, flower,
                    energySystem),
            };

            _currentState = _states[0];
            _currentState.Enter();
        }

        public void SwitchState<T>() where T : ISpiderState
        {
            ISpiderState newState = _states.FirstOrDefault(state => state is T);

            //Debug.Log($"NewState : {newState.GetType().Name}");

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