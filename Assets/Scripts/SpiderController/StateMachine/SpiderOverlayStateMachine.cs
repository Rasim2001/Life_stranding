using System.Collections.Generic;
using System.Linq;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.GravityGun;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using SpiderController.StateMachine.OverlayStates;
using SpiderController.Trajectory;
using UnityEngine;

namespace SpiderController.StateMachine
{
    public class SpiderOverlayStateMachine : ISpiderStateMachine
    {
        private readonly List<ISpiderState> _states;

        private ISpiderState _currentState;

        public SpiderOverlayStateMachine(IInputService inputService, IGravityGunDisplayer displayer,
            ICameraProviderService cameraProviderService, IStaticDataService staticDataService,
            StateMachineData stateMachineData, Transform rotationPlaneTransform)
        {
            _states = new List<ISpiderState>()
            {
                new EmptyOverlayState(this, inputService),
                new GravityGunOverlayState(this, inputService, cameraProviderService, displayer, stateMachineData,
                    rotationPlaneTransform, staticDataService.GravityGunStaticData),
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