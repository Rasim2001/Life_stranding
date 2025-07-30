using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController.StateMachine.States.Airborn
{
    public class AirbornState : MovementState
    {
        private readonly GroundChecker _groundChecker;

        protected AirbornState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, Spider spider, StateMachineData stateMachineData,
            LegDataStruct[] legs) : base(stateMachine, inputService, staticDataService, spider, stateMachineData, legs)
        {
            _groundChecker = spider.GroundChecker;
        }

        public override void Enter()
        {
            base.Enter();

            Data.Speed = SpiderStaticData.Speed;

            _groundChecker.SetAirbornLegState();
        }


        public override void Update()
        {
            base.Update();

            SpendEnergy();

            Data.YVelocity -= SpiderStaticData.BaseGravity * Data.AirbornSpeed * Time.deltaTime;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            AlignRotationInFlight();
        }


        private void AlignRotationInFlight()
        {
            Vector3 currentEuler = Rigidbody.rotation.eulerAngles;

            float deltaX = Mathf.DeltaAngle(0, currentEuler.x);
            float deltaZ = Mathf.DeltaAngle(0, currentEuler.z);

            float alignX = -deltaX * Time.fixedDeltaTime;
            float alignZ = -deltaZ * Time.fixedDeltaTime;

            Quaternion deltaRotation = Quaternion.Euler(alignX, 0, alignZ);
            Rigidbody.MoveRotation(Rigidbody.rotation * deltaRotation);
        }
    }
}