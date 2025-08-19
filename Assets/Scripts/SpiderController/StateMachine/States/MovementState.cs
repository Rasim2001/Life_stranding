using System.Collections.Generic;
using System.Linq;
using Infastructure.Services.Input;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using SpiderController.UI;
using SpiderController.UI.Health;
using UnityEngine;

namespace SpiderController.StateMachine.States
{
    public class MovementState : ISpiderState
    {
        protected readonly ISpiderStateMachine StateMachine;
        protected readonly StateMachineData Data;
        protected readonly Spider Spider;
        protected readonly LegDataStruct[] Legs;
        protected readonly Flower Flower;

        protected IInputService InputService => _inputService;
        protected Rigidbody Rigidbody => Spider.Rigidbody;
        protected SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private SpiderHealth SpiderHealth => Spider.SpiderUI.SpiderHealth;
        private EnergyBarUI EnergyBar => Spider.SpiderUI.EnergyBar;

        private readonly IStaticDataService _staticDataService;
        private readonly IInputService _inputService;
        private readonly float _legMoveDeadzone = 0.04f;

        private readonly List<BackLegRaycast> _backLegs;


        protected MovementState(
            ISpiderStateMachine stateMachine,
            IInputService inputService,
            IStaticDataService staticDataService,
            Spider spider,
            StateMachineData stateMachineData,
            LegDataStruct[] legs,
            Flower flower)
        {
            _inputService = inputService;
            _staticDataService = staticDataService;
            Flower = flower;

            StateMachine = stateMachine;
            Spider = spider;
            Data = stateMachineData;
            Legs = legs;

            _backLegs = Legs
                .Select(x => x.Raycast.GetComponent<BackLegRaycast>())
                .Where(x => x != null)
                .ToList();
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

            Data.RotationAmount = Data.Input.x * SpiderStaticData.LerpForwardSpeed;
        }

        public virtual void Update()
        {
            InputHandler();
            TryMoveLegs();
            CheckFlowerAndReduceHp();
            BackLegHandle();
        }

        public virtual void FixedUpdate()
        {
            MoveBodySpider();
            RotateTowardsMoveDirection();

            if (!Mathf.Approximately(Data.YVelocity, 0))
                return;

            AdjustBodyHeight();
            AdjustBodyOrientation();
        }

        public void LateUpdate()
        {
        }

        private void InputHandler()
        {
            if (_inputService.RightMousePressed)
            {
                Spider.SpiderUI.MagnetIndicatorUI.Show();

                Flower.IsFreezingOnPlatform = true;
                Data.IsMouseHolding = true;
            }

            else if (_inputService.RightMouseUp)
            {
                Spider.SpiderUI.MagnetIndicatorUI.Hide();

                Flower.IsFreezingOnPlatform = false;
                Data.IsMouseHolding = false;
            }

            if (Data.IsMouseHolding)
                SpendEnergy(SpiderStaticData.EnergySpendFreezingFlowerSpeed);

            if (Data.EnergyFillAmount <= 0 && Flower.IsFreezingOnPlatform)
                Flower.IsFreezingOnPlatform = false;
        }


        protected bool IsInputZero() =>
            Mathf.Abs(Data.Input.z) < Mathf.Epsilon;

        protected bool IsFastRunPressed() =>
            _inputService.IsLeftShiftPressed;

        protected bool IsFastRunUp() =>
            _inputService.IsLeftShiftUp;

        protected bool SlowdownPressed() =>
            _inputService.CtrlPressed;

        protected bool SlowdownUp() =>
            _inputService.CtrlUp;


        protected void SpendEnergy(float speed)
        {
            if (Data.EnergyFillAmount >= 0)
            {
                Data.EnergyFillAmount -= Time.deltaTime * speed /
                                         SpiderStaticData.EnergyFillAmount;

                EnergyBar.SetEnergyValue(Data.EnergyFillAmount);
            }
        }

        protected void RestoreEnergy(float speed)
        {
            if (Data.EnergyFillAmount < 1)
            {
                Data.EnergyFillAmount += Time.deltaTime * speed /
                                         SpiderStaticData.EnergyFillAmount;

                EnergyBar.SetEnergyValue(Data.EnergyFillAmount);
            }
        }


        protected virtual void TryMoveLegs()
        {
            for (int index = 0; index < Legs.Length; index++)
            {
                ref LegDataStruct legData = ref Legs[index];

                if (!CanMove(index))
                    continue;

                float dist = Vector3.Distance(legData.Leg.Position, legData.Raycast.Position);

                if (!legData.Leg.IsMoving && dist < SpiderStaticData.StepLength + _legMoveDeadzone)
                    continue;

                if (legData.Raycast.IsGrounded)
                    legData.Leg.MoveTo(legData.Raycast.Position);
            }
        }

        private void CheckFlowerAndReduceHp()
        {
            if (Flower.IsOnPlatform == false)
                SpiderHealth.TakeDamage(SpiderStaticData.DamageAmount);
        }

        private void BackLegHandle()
        {
            if (_backLegs == null)
                return;

            if (Data.Input.z > 0)
            {
                foreach (BackLegRaycast backLeg in _backLegs)
                    backLeg.SetForwardStateLeg();
            }
            else if (Data.Input.z < 0)
            {
                foreach (BackLegRaycast backLeg in _backLegs)
                    backLeg.SetBackStateLeg();
            }
        }

        private void MoveBodySpider()
        {
            Vector3 forwardMovement = Spider.transform.forward * Data.Velocity.z;
            Vector3 verticalMovement = Spider.transform.up * Data.YVelocity;
            Vector3 jerkMovement = Spider.transform.forward * Data.XVelocity;

            Vector3 newVelocity = forwardMovement + verticalMovement + jerkMovement;

            Rigidbody.linearVelocity = Data.IsStandingUpAfterFalling ? Vector3.zero : newVelocity;
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
            int count = 0;

            Transform spiderTransform = Spider.transform;
            for (int i = 0; i < Legs.Length; i++)
            {
                avgLegPos += Legs[i].Raycast.Position;
                count++;
            }

            if (count == 0)
                return;

            avgLegPos = spiderTransform.InverseTransformPoint(avgLegPos / count);
            float targetY = avgLegPos.y + Data.DistanceFromGround;
            Vector3 localPos = spiderTransform.InverseTransformPoint(Rigidbody.position);

            float newLocalY = Mathf.Lerp(localPos.y, targetY,
                Time.fixedDeltaTime * SpiderStaticData.LerpSpeedFromGround);

            float deltaY = newLocalY - localPos.y;
            float localVerticalVelocity = deltaY / Time.fixedDeltaTime;

            Vector3 localVelocity = spiderTransform.InverseTransformDirection(Rigidbody.linearVelocity);
            localVelocity.y = localVerticalVelocity;

            Rigidbody.linearVelocity = spiderTransform.TransformDirection(localVelocity);
        }


        private void RotateTowardsMoveDirection()
        {
            float totalRotationZ = Data.Input.z >= 0 ? Data.RotationAmount : -Data.RotationAmount;

            Vector3 rotationAxis = Spider.transform.up;
            float rotationSpeedRad = totalRotationZ * Mathf.Deg2Rad;

            Rigidbody.angularVelocity = rotationAxis * rotationSpeedRad;
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

                normalSum -= normal;
                count++;
            }

            if (count == 0)
                return;

            Vector3 averageNormal = (normalSum / count).normalized;

            Quaternion targetRotation =
                Quaternion.FromToRotation(Spider.transform.up, averageNormal) * Rigidbody.rotation;

            Quaternion smoothedRotation = Quaternion.Slerp(Rigidbody.rotation, targetRotation,
                Time.fixedDeltaTime * SpiderStaticData.LerpSpeedFromGround);

            Quaternion deltaRotation = smoothedRotation * Quaternion.Inverse(Rigidbody.rotation);

            deltaRotation.ToAngleAxis(out float angleDeg, out Vector3 axis);
            if (angleDeg > 180f)
                angleDeg -= 360f;

            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector3 angularVel = axis.normalized * (angleRad / Time.fixedDeltaTime);

            Rigidbody.angularVelocity += angularVel;
        }
    }
}