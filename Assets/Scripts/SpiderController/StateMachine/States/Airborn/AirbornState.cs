using Infastructure.Services.PlayerInput;
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
            LegDataStruct[] legs, Flower flower) : base(stateMachine, inputService, staticDataService, spider,
            stateMachineData, legs, flower)
        {
            _groundChecker = spider.GroundChecker;
        }

        public override void Enter()
        {
            base.Enter();

            SetAirbornLegs();
            Data.Speed = SpiderStaticData.Speed;

            _groundChecker.SetAirbornLegState();
        }

        public override void Exit()
        {
            base.Exit();

            SetGroundLegs();
        }


        public override void Update()
        {
            base.Update();

            SpendEnergy(SpiderStaticData.EnergySpendAirbornSpeed);

            Data.YVelocity -= SpiderStaticData.BaseGravity * Data.AirbornSpeed * Time.deltaTime;
        }

        protected override void TryMoveLegs()
        {
            for (int index = 0; index < Legs.Length; index++)
            {
                ref LegDataStruct legData = ref Legs[index];
                legData.Leg.MoveTo(legData.Raycast.AirbornPosition);
            }
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

            float alignX = -deltaX * Time.fixedDeltaTime / 2;
            float alignZ = -deltaZ * Time.fixedDeltaTime / 2;

            Quaternion deltaRotation = Quaternion.Euler(alignX, 0, alignZ);
            Rigidbody.MoveRotation(Rigidbody.rotation * deltaRotation);
        }


        private void SetAirbornLegs()
        {
            for (int index = 0; index < Legs.Length; index++)
            {
                ref LegDataStruct legData = ref Legs[index];
                legData.Leg.IsAirbornState = true;
            }
        }

        private void SetGroundLegs()
        {
            for (int index = 0; index < Legs.Length; index++)
            {
                ref LegDataStruct legData = ref Legs[index];
                legData.Leg.IsAirbornState = false;
            }
        }
    }
}