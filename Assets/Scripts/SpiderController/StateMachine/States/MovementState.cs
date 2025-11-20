using System.Collections.Generic;
using System.Linq;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.SpiderMove;
using SpiderController.UI;
using UnityEngine;

namespace SpiderController.StateMachine.States
{
    public class MovementState : ISpiderState
    {
        protected readonly ISpiderStateMachine StateMachine;
        protected readonly StateMachineData Data;
        protected readonly Spider Spider;
        protected readonly LegDataStruct[] Legs;
        protected readonly EnergySystem EnergySystem;
        protected IInputService InputService => _inputService;
        protected Rigidbody Rigidbody => Spider.Rigidbody;
        protected SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;
        protected EnergyBarUI EnergyBarUI => Spider.SpiderUI.EnergyBar;
        protected Transform CameraTransform => Spider.CameraProviderService.CameraTransform;

        private readonly Flower _flower;
        private readonly IStaticDataService _staticDataService;
        private readonly ICutSceneService _cutSceneService;
        private readonly IInputService _inputService;
        private readonly float _legMoveDeadzone = 0.04f;

        private Vector3 _movementDirection;


        protected MovementState(
            ISpiderStateMachine stateMachine,
            IInputService inputService,
            IStaticDataService staticDataService,
            ICutSceneService cutSceneService,
            Spider spider,
            StateMachineData stateMachineData,
            LegDataStruct[] legs,
            Flower flower,
            EnergySystem energySystem)
        {
            _inputService = inputService;
            _staticDataService = staticDataService;
            _cutSceneService = cutSceneService;
            _flower = flower;
            EnergySystem = energySystem;

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

            float lerpForwardSpeed = _cutSceneService.IsActive && _cutSceneService.LerpForwardSpeed != 0
                ? _cutSceneService.LerpForwardSpeed
                : SpiderStaticData.LerpForwardSpeed;

            Data.RotationAmount = Data.Input.x * lerpForwardSpeed;
        }

        public virtual void Update()
        {
            TryMoveLegs();
            UpdateTerranTime();
        }

        public virtual void FixedUpdate()
        {
            MoveBodySpider();

            if (!Mathf.Approximately(Data.YVelocity, 0))
                return;

            AdjustBodyHeight();
            AdjustBodyOrientation();
        }

        public void LateUpdate()
        {
        }


        protected bool IsInputZero() =>
            Data.Input.sqrMagnitude < Mathf.Epsilon;

        protected bool IsFastRunPressed() =>
            _inputService.IsLeftShiftPressed;

        protected bool IsFastRunUp() =>
            _inputService.IsLeftShiftUp;

        protected bool SlowdownPressed() =>
            _inputService.CtrlPressed;

        protected bool SlowdownUp() =>
            _inputService.CtrlUp;


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

        protected bool IsNotMoveableLayer() =>
            Legs.Select(x => x.Raycast).Any(x => x.IsNotMoveableLayer);

        protected void SetSpeed(float newValue) =>
            Data.Speed = newValue;

        protected virtual void MoveBodySpider()
        {
            Vector3 worldUp = CameraTransform.up;
            Vector3 camForward = Vector3.ProjectOnPlane(CameraTransform.forward, worldUp).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(CameraTransform.right, worldUp).normalized;

            Vector3 forwardMovement = camForward * Data.Velocity.z;
            Vector3 movementX = camRight * Data.Velocity.x;

            _movementDirection = forwardMovement + movementX;

            Vector3 jerkMovement = camForward * Data.XVelocity;
            Vector3 verticalMovement = Spider.transform.up * Data.YVelocity;
            Vector3 explosionVector = Data.ExplosionVector;

            Vector3 newVelocity = _movementDirection + verticalMovement + jerkMovement + explosionVector;

            Rigidbody.linearVelocity =
                Data.IsStandingUpAfterFalling || IsNotMoveableLayer() ? Vector3.zero : newVelocity;
        }

        private void UpdateTerranTime()
        {
            if (Data.TerrainTimer > 0)
            {
                Data.TerrainTimer -= Time.deltaTime;

                Spider.SpiderUI.ReloadUI.SetValue(Data.TerrainTimer / Data.TerrainTimerDefault);

                if (Data.TerrainTimer <= 0)
                {
                    Spider.SpiderUI.ReloadUI.ShowHologram();
                    Data.TerrainTimer = Mathf.NegativeInfinity;
                }
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
            int count = 0;

            Transform spiderTransform = Spider.transform;
            for (int i = 0; i < Legs.Length; i++)
            {
                if (Legs[i].Raycast.IsGrounded)
                {
                    avgLegPos += Legs[i].Raycast.Position;
                    count++;
                }
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
            Vector3 angularExplosionVector = Data.ExplosionAngularVector;

            Rigidbody.angularVelocity = angularVel + angularExplosionVector;
        }
    }
}