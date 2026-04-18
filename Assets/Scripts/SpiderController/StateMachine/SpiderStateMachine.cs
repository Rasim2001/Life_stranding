using System.Collections.Generic;
using System.Linq;
using Infastructure.Factories;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.Platform;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States;
using SpiderController.StateMachine.States.Airborn;
using SpiderController.StateMachine.States.Ground;
using SpiderController.StateMachine.States.Ground.Aiming;
using SpiderController.StateMachine.States.Rewind;

namespace SpiderController.StateMachine
{
    public class SpiderStateMachine : ISpiderStateMachine
    {
        private readonly List<ISpiderState> _states;

        private ISpiderState _currentState;

        public SpiderStateMachine(
            IDiFactory diFactory,
            SpiderStateContext stateContext,
            SpiderServiceContext serviceContext,
            EnergySystem energySystem,
            SpiderRewindRecorder recorder,
            PlatformSelector platformSelector)
        {
            _states = new List<ISpiderState>()
            {
                diFactory.Create<IdlingState>(this, serviceContext, stateContext, energySystem),
                diFactory.Create<RunningState>(this, serviceContext, stateContext, energySystem),
                diFactory.Create<FastRunningState>(this, serviceContext, stateContext, energySystem),
                diFactory.Create<JumpingState>(this, serviceContext, stateContext, energySystem),
                diFactory.Create<FallingState>(this, serviceContext, stateContext, energySystem),
                diFactory.Create<FallingWithoutEnergyState>(this, serviceContext, stateContext, energySystem),
                diFactory.Create<JerkState>(this, serviceContext, stateContext, energySystem),
                diFactory.Create<SlowdownState>(this, serviceContext, stateContext, energySystem),
                diFactory.Create<FallingWithControlState>(this, serviceContext, stateContext, energySystem),
                diFactory.Create<RecoveryState>(this, serviceContext, stateContext, energySystem),
                diFactory.Create<AimIdlingState>(this, serviceContext, stateContext, energySystem),
                diFactory.Create<AimRunningState>(this, serviceContext, stateContext, energySystem),
                diFactory.Create<RewindState>(this, serviceContext, stateContext, energySystem, recorder, platformSelector),
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

        public bool IsCurrentState<T>() where T : ISpiderState =>
            _currentState is T;

        public void HandleInput() => _currentState.HandleInput();

        public void Update() => _currentState.Update();

        public void FixedUpdate() => _currentState.FixedUpdate();

        public void LateUpdate() => _currentState.LateUpdate();
    }
}