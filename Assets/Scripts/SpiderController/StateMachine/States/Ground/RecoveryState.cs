using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;

namespace SpiderController.StateMachine.States.Ground
{
    public class RecoveryState : MovementState
    {
        private CancellationTokenSource _cts;

        protected RecoveryState(
            ISpiderStateMachine stateMachine,
            SpiderServiceContext serviceContext,
            SpiderStateContext stateContext,
            EnergySystem energySystem) :
            base(stateMachine, serviceContext, stateContext, energySystem)
        {
        }

        public override void Enter()
        {
            base.Enter();

            _cts = new CancellationTokenSource();

            MoveToLastPositionAsync().Forget();
        }


        public override void Exit()
        {
            base.Exit();

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }


        private async UniTask MoveToLastPositionAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: _cts.Token);

            if (IsNotMoveableLayer() == false)
            {
                Rigidbody.linearVelocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;

                StateMachine.SwitchState<IdlingState>();

                return;
            }

            Transform.position = Data.LastValidGroundPosition;
            Transform.rotation = Data.LastValidGroundRotation;

            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: _cts.Token);

            StateMachine.SwitchState<IdlingState>();
        }
    }
}