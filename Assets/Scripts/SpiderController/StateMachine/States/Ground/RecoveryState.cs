using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController.StateMachine.States.Ground
{
    public class RecoveryState : MovementState
    {
        public RecoveryState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(stateMachine, inputService,
            staticDataService, spider, stateMachineData, legs, flower, energySystem)
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
                Spider.Rigidbody.linearVelocity = Vector3.zero;
                Spider.Rigidbody.angularVelocity = Vector3.zero;

                StateMachine.SwitchState<IdlingState>();

                return;
            }

            Spider.Rigidbody.linearVelocity = Vector3.zero;
            Spider.Rigidbody.angularVelocity = Vector3.zero;

            Spider.Rigidbody.position = Data.LastValidGroundPosition;
            Spider.Rigidbody.rotation = Data.LastValidGroundRotation;

            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            StateMachine.SwitchState<IdlingState>();
        }
    }
}