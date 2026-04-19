using System.Collections.Generic;
using System.Linq;
using Infastructure.Factories;
using SpiderController.StateMachine.OverlayStates;
using UnityEngine;

namespace SpiderController.StateMachine
{
    public class SpiderOverlayStateMachine : ISpiderStateMachine
    {
        private readonly List<ISpiderState> _states;

        private ISpiderState _currentState;

        public SpiderOverlayStateMachine(IDiFactory diFactory, SpiderStateContext stateContext)
        {
            _states = new List<ISpiderState>()
            {
                diFactory.Create<EmptyOverlayState>(this),
                diFactory.Create<GravityGunOverlayState>(this, stateContext),
                diFactory.Create<TeleportOverlayState>(this, stateContext),
                diFactory.Create<AimingOverlayState>(this, stateContext)
            };

            _currentState = _states[0];
            _currentState.Enter();
        }

        public bool IsCurrentState<T>() where T : ISpiderState =>
            _currentState is T;

        public void HandleInput() => _currentState.HandleInput();

        public void Update() => _currentState.Update();

        public void FixedUpdate() => _currentState.FixedUpdate();

        public void LateUpdate() => _currentState.LateUpdate();

        public void SwitchState<T>() where T : ISpiderState
        {
            ISpiderState newState = _states.FirstOrDefault(state => state is T);

            //Debug.Log($"NewState : {newState.GetType().Name}");

            _currentState.Exit();
            _currentState = newState;
            _currentState.Enter();
        }
    }
}