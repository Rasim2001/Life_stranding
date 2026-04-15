using System;
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

            MoveToLastPositionAsync().Forget();
        }


        private async UniTask MoveToLastPositionAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(3));

            if (IsNotMoveableLayer() == false)
            {
                Rigidbody.linearVelocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;

                StateMachine.SwitchState<IdlingState>();

                return;
            }

            /*Spider.Rigidbody.linearVelocity = Vector3.zero;
            Spider.Rigidbody.angularVelocity = Vector3.zero;*/

            Transform.position = Data.LastValidGroundPosition;
            Transform.rotation = Data.LastValidGroundRotation;

            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            StateMachine.SwitchState<IdlingState>();
        }
    }
}