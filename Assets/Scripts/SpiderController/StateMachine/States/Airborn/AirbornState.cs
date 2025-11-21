using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController.StateMachine.States.Airborn
{
    public class AirbornState : MovementState
    {
        protected AirbornState(ISpiderStateMachine stateMachine, IInputService inputService,
            IStaticDataService staticDataService, ICutSceneService cutSceneService, Spider spider,
            StateMachineData stateMachineData,
            LegDataStruct[] legs, Flower flower, EnergySystem energySystem) : base(stateMachine, inputService,
            staticDataService, cutSceneService, spider,
            stateMachineData, legs, flower, energySystem)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Data.OnTotalWeightChanged += WeightChanged;

            SetAirbornLegs();
            SetSpeed(SpiderStaticData.Speed);
        }

        public override void Exit()
        {
            base.Exit();

            Data.OnTotalWeightChanged -= WeightChanged;

            SetGroundLegs();
        }

        private void WeightChanged() =>
            SetSpeed(SpiderStaticData.Speed);


        public override void Update()
        {
            base.Update();

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

        protected void SetCrossLegs()
        {
            for (int index = 0; index < Legs.Length; index++)
            {
                ref LegDataStruct legData = ref Legs[index];
                legData.Leg.IsCrossingLeg = true;
            }
        }

        protected void SetUncrossLegs()
        {
            for (int index = 0; index < Legs.Length; index++)
            {
                ref LegDataStruct legData = ref Legs[index];
                legData.Leg.IsCrossingLeg = false;
            }
        }

        protected void ShakeCamera()
        {
            float currentY = Spider.transform.position.y;
            float distanceFalling = Mathf.Abs(Data.GlobalY - currentY);

            if (distanceFalling > SpiderStaticData.MinShakeDistance)
            {
                Data.GlobalY = 0;
                Data.OnShakeHappened?.Invoke(distanceFalling);
            }
        }

        protected void OnTriggerEnterWithWater(Collider obj)
        {
            GameObject prefab = Spider.WaterStaticData.WaterSplashPrefab;

            Object.Instantiate(prefab, Spider.transform.position + new Vector3(0, 0), Quaternion.identity);
        }


        private void AlignRotationInFlight()
        {
            Quaternion currentRotation = Rigidbody.rotation;
            Vector3 worldUp = Vector3.up;

            Vector3 forward = Spider.transform.forward;
            Vector3 flatForward = Vector3.ProjectOnPlane(forward, worldUp);

            flatForward.Normalize();
            Quaternion targetRotation = Quaternion.LookRotation(flatForward, worldUp);

            float degreesPerSecond = 180;
            float maxDegreesDelta = degreesPerSecond * Time.fixedDeltaTime;

            Quaternion newRotation = Quaternion.RotateTowards(
                currentRotation,
                targetRotation,
                maxDegreesDelta);

            Rigidbody.MoveRotation(newRotation);
        }


        private void SetAirbornLegs()
        {
            for (int index = 0; index < Legs.Length; index++)
            {
                ref LegDataStruct legData = ref Legs[index];
                legData.Leg.IsAirbornState = true;
                legData.Raycast.SetAirbornState();
            }
        }

        private void SetGroundLegs()
        {
            for (int index = 0; index < Legs.Length; index++)
            {
                ref LegDataStruct legData = ref Legs[index];
                {
                    legData.Leg.IsAirbornState = false;
                    legData.Raycast.SetGroundState();
                }
            }
        }
    }
}