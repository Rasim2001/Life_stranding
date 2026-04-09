using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Ground;
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
            stateMachineData, legs, energySystem)
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

        private void WeightChanged()
        {
            SetSpeed(SpiderStaticData.Speed);
        }


        public override void Update()
        {
            base.Update();

            Data.YVelocity -= SpiderStaticData.BaseGravity * Data.AirbornSpeed * Time.deltaTime;

            if (InputService.JerkPressed && Data.CurrentEnergyFillAmount > 0 && !Data.IsStandingUpAfterFalling &&
                Spider.AbilityService.IsExploredAbility(ProductType.JerkSkillProduct))
                StateMachine.SwitchState<JerkState>();
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
            Vector3 currentUp = currentRotation * Vector3.up;

            Vector3 worldUp = Vector3.up;

            Vector3 axis = Vector3.Cross(currentUp, worldUp);
            float angle = Vector3.Angle(currentUp, worldUp);

            if (angle < 0.1f)
            {
                float damping = 5f;
                Rigidbody.angularVelocity = Vector3.Lerp(
                    Rigidbody.angularVelocity,
                    Vector3.zero,
                    damping * Time.fixedDeltaTime);
                return;
            }

            if (axis.sqrMagnitude < 1e-6f)
                axis = Spider.transform.right;

            axis.Normalize();

            float angleRad = angle * Mathf.Deg2Rad;
            float alignGain = 5f;

            Vector3 desiredAngularVelocity = axis * (angleRad * alignGain);

            float maxAngularSpeed = 10f;
            if (desiredAngularVelocity.magnitude > maxAngularSpeed)
                desiredAngularVelocity = desiredAngularVelocity.normalized * maxAngularSpeed;

            float blend = 10f * Time.fixedDeltaTime;
            Rigidbody.angularVelocity = Vector3.Lerp(
                Rigidbody.angularVelocity,
                desiredAngularVelocity,
                blend);
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