using _2;
using Infastructure.Services.Input;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController.StateMachine.States
{
    public class MovementState : ISpiderState
    {
        protected readonly ISpiderStateMachine StateMachine;
        protected readonly StateMachineData Data;
        protected readonly Spider Spider;
        protected readonly LegDataStruct[] Legs;
        protected Rigidbody Rigidbody => Spider.Rigidbody;
        protected SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;
        protected IInputService InputService => _inputService;

        private readonly IStaticDataService _staticDataService;
        private readonly IInputService _inputService;

        protected MovementState(
            ISpiderStateMachine stateMachine,
            IInputService inputService,
            IStaticDataService staticDataService,
            Spider spider,
            StateMachineData stateMachineData,
            LegDataStruct[] legs
        )
        {
            _inputService = inputService;
            _staticDataService = staticDataService;

            StateMachine = stateMachine;
            Spider = spider;
            Data = stateMachineData;
            Legs = legs;
        }


        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void HandleInput()
        {
            Data.Input = _inputService.InputVector;
            Data.Velocity = Data.Input * Data.Speed;
        }

        public virtual void Update()
        {
            TryMoveLegs();
        }

        public void FixedUpdate()
        {
            MoveBodySpider();
            RotateTowardsMoveDirection();

            if (!Mathf.Approximately(Data.YVelocity, 0))
                return;

            AdjustBodyHeight();
            AdjustBodyOrientation();
        }

        protected bool IsInputZero() =>
            Mathf.Abs(Data.Input.z) < Mathf.Epsilon;

        protected bool IsFastRunPressed() =>
            _inputService.IsLeftShiftPressed;

        protected bool IsFastRunUp() =>
            _inputService.IsLeftShiftUp;

        private void MoveBodySpider()
        {
            Vector3 forwardMovement = Spider.transform.forward * (Data.Velocity.z * Time.fixedDeltaTime);
            Vector3 verticalMovement = new Vector3(0, Data.YVelocity, 0) * Time.fixedDeltaTime;

            Vector3 newPosition = Spider.Rigidbody.position + forwardMovement + verticalMovement;

            Rigidbody.MovePosition(newPosition);
        }

        private void RotateTowardsMoveDirection()
        {
            if (Mathf.Abs(Data.Input.x) > Mathf.Epsilon)
            {
                float rotationAmount = Data.Input.x * SpiderStaticData.LerpForwardSpeed * Time.fixedDeltaTime;
                float totalRotation = Data.Input.z >= 0 ? rotationAmount : -rotationAmount;

                Quaternion deltaRotation = Quaternion.Euler(0, totalRotation, 0);
                Quaternion newRotation = Rigidbody.rotation * deltaRotation;

                Rigidbody.MoveRotation(newRotation);
            }
        }

        private void TryMoveLegs()
        {
            for (int index = 0; index < Legs.Length; index++)
            {
                ref LegDataStruct legData = ref Legs[index];

                if (!CanMove(index))
                    continue;

                if (!legData.Leg.IsMoving &&
                    Vector3.Distance(legData.Leg.Position, legData.Raycast.Position) < SpiderStaticData.StepLength)
                    continue;

                if (legData.Raycast.IsGrounded)
                    legData.Leg.MoveTo(legData.Raycast.Position);
            }
        }

        private bool CanMove(int legIndex)
        {
            int legsCount = Legs.Length;
            LegDataStruct n1 = Legs[(legIndex + legsCount - 1) % legsCount];
            LegDataStruct n2 = Legs[(legIndex + 1) % legsCount];

            return !n1.Leg.IsMoving && !n2.Leg.IsMoving;
        }

        private void AdjustBodyHeight()
        {
            Vector3 avgLegPos = Vector3.zero;

            for (int i = 0; i < Legs.Length; i++)
                avgLegPos += Legs[i].Raycast.Position;

            avgLegPos /= Legs.Length;

            Vector3 localAvgLegPos = Spider.transform.InverseTransformPoint(avgLegPos);
            float targetY = localAvgLegPos.y + SpiderStaticData.DistanceFromGround;

            Vector3 localPos = Spider.transform.InverseTransformPoint(Rigidbody.position);
            localPos.y = Mathf.Lerp(localPos.y, targetY, Time.fixedDeltaTime * SpiderStaticData.LerpSpeedFromGround);

            Vector3 worldPos = Spider.transform.TransformPoint(localPos);
            Rigidbody.MovePosition(worldPos);
        }

        private void AdjustBodyOrientation()
        {
            Vector3[] legPositions = new Vector3[Legs.Length];
            for (int i = 0; i < Legs.Length; i++)
                legPositions[i] = Legs[i].Raycast.Position;

            Vector3 normalSum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < Legs.Length; i++)
            {
                if (!Legs[i].Raycast.IsGrounded)
                    continue;

                int i1 = (i + 1) % Legs.Length;
                int i2 = (i + 2) % Legs.Length;
                int i3 = (i + 3) % Legs.Length;

                Vector3 v1 = legPositions[i2] - legPositions[i1];
                Vector3 v2 = legPositions[i3] - legPositions[i1];
                Vector3 normal = Vector3.Cross(v1, v2).normalized;

                /*if (normal.y < 0)
                    normal = -normal;*/

                normalSum -= normal;
                count++;
            }

            Vector3 averageNormal = normalSum / count;
            averageNormal.Normalize();

            Quaternion targetRotation =
                Quaternion.FromToRotation(Spider.transform.up, averageNormal) * Rigidbody.rotation;

            Quaternion smoothedRotation = Quaternion.Slerp(Rigidbody.rotation, targetRotation,
                Time.fixedDeltaTime * SpiderStaticData.LerpSpeedFromGround);

            Rigidbody.MoveRotation(smoothedRotation);
        }
    }
}