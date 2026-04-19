using Infastructure.Services.Ability;
using Infastructure.Services.Magnet;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Infastructure.StaticData.Spider;
using SpiderController.SpiderMove;
using SpiderController.StateMachine.States.Rewind;
using SpiderController.Thruster;
using SpiderController.UI.Health;
using UnityEngine;

namespace SpiderController.StateMachine.States
{
    public class MovementState : ISpiderState
    {
        protected readonly ISpiderStateMachine StateMachine;
        protected readonly EnergySystem EnergySystem;
        protected readonly SpiderStateContext StateContext;
        protected readonly SpiderServiceContext ServiceContext;

        protected IInputService InputService => ServiceContext.InputService;
        protected IAbilityService AbilityService => ServiceContext.AbilityService;
        protected IMagnetFreezingService MagnetFreezingService => ServiceContext.MagnetFreezingService;
        protected IWindowService WindowService => ServiceContext.WindowService;
        protected ThrusterSystem ThrusterSystem => StateContext.ThrusterSystem;
        protected StateMachineData Data => StateContext.Data;
        protected LegDataStruct[] Legs => StateContext.Legs;
        protected SpiderStaticData SpiderStaticData => ServiceContext.StaticDataService.SpiderStaticData;
        protected Transform Transform => StateContext.Transform;
        protected Transform CameraTransform => ServiceContext.CameraProviderService.CameraTransform;
        protected Rigidbody Rigidbody => StateContext.Rigidbody;
        protected SpiderUI SpiderUI => StateContext.SpiderUI;

        private readonly Vector3[] _legPositions;
        private readonly float _legMoveDeadzone = 0.04f;

        private int _legCount;

        private bool _isAdhesionActivated;
        private float _adhesionTime = 0.1f;


        protected MovementState(
            ISpiderStateMachine stateMachine,
            SpiderServiceContext serviceContext,
            SpiderStateContext stateContext,
            EnergySystem energySystem)
        {
            ServiceContext = serviceContext;
            EnergySystem = energySystem;
            StateMachine = stateMachine;
            StateContext = stateContext;

            _legPositions = new Vector3[StateContext.Legs.Length];
        }


        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void HandleInput()
        {
            Data.Input = InputService.InputVector;
            Data.Velocity = Data.Input * Data.Speed;
        }

        public virtual void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
                StateMachine.SwitchState<RewindState>();

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
            InputService.IsLeftShiftPressed;

        protected bool IsFastRunUp() =>
            InputService.IsLeftShiftUp;

        protected bool SlowdownPressed() =>
            InputService.CtrlPressed;

        protected bool SlowdownUp() =>
            InputService.CtrlUp;


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

        protected bool IsNotMoveableLayer()
        {
            for (int i = 0; i < Legs.Length; i++)
                if (Legs[i].Raycast.IsNotMoveableLayer)
                    return true;

            return false;
        }

        protected void SetSpeed(float newValue) =>
            Data.Speed = newValue;

        protected virtual Vector3 GetMovementY() =>
            Transform.up * Data.YVelocity;

        private void MoveBodySpider()
        {
            Vector3 worldUp = CameraTransform.up;
            Vector3 camForward = Vector3.ProjectOnPlane(CameraTransform.forward, worldUp).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(CameraTransform.right, worldUp).normalized;

            Vector3 forwardMovement = camForward * Data.Velocity.z;
            Vector3 movementX = camRight * Data.Velocity.x;

            Vector3 movementDirection = forwardMovement + movementX;

            Vector3 jerkMovement = camForward * Data.XVelocity;
            Vector3 verticalMovement = GetMovementY();
            Vector3 explosionVector = Data.ExplosionVector;

            Vector3 newVelocity = movementDirection + verticalMovement + jerkMovement + explosionVector;

            Rigidbody.linearVelocity =
                Data.IsStandingUpAfterFalling || IsNotMoveableLayer() ? Vector3.zero : newVelocity;
        }

        private void UpdateTerranTime()
        {
            if (Data.TerrainTimer > 0)
            {
                Data.TerrainTimer -= Time.deltaTime;

                SpiderUI.ReloadUI.SetValue(Data.TerrainTimer / Data.TerrainTimerDefault);

                if (Data.TerrainTimer <= 0)
                {
                    SpiderUI.ReloadUI.ShowHologram();
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

            Transform spiderTransform = Transform;
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
            for (int i = 0; i < Legs.Length; i++)
                _legPositions[i] = Legs[i].Raycast.Position;

            Vector3 normalSum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < Legs.Length; i++)
            {
                if (!Legs[i].Raycast.IsGrounded)
                    continue;

                int i1 = (i + 1) % Legs.Length;
                int i2 = (i + 2) % Legs.Length;
                int i3 = (i + 3) % Legs.Length;

                Vector3 v1 = _legPositions[i2] - _legPositions[i1];
                Vector3 v2 = _legPositions[i3] - _legPositions[i1];
                Vector3 normal = Vector3.Cross(v1, v2).normalized;

                normalSum -= normal;
                count++;
            }

            if (count == 0)
                return;

            if (_isAdhesionActivated)
            {
                if (_adhesionTime > 0)
                {
                    CalculateAdhesion();
                    _adhesionTime -= Time.deltaTime;
                }
                else
                {
                    _isAdhesionActivated = false;
                    _adhesionTime = 0.1f;
                }
            }
            else
            {
                if (count != Legs.Length)
                {
                    _isAdhesionActivated = true;

                    CalculateAdhesion();
                }
                else
                    CalculateGroundWithAllLegs(normalSum, count);
            }
        }

        private void CalculateGroundWithAllLegs(Vector3 normalSum, int count)
        {
            Vector3 averageNormal = (normalSum / count).normalized;

            Quaternion targetRotation =
                Quaternion.FromToRotation(Transform.up, averageNormal) * Rigidbody.rotation;

            Quaternion smoothedRotation = Quaternion.Slerp(Rigidbody.rotation, targetRotation,
                Time.fixedDeltaTime * SpiderStaticData.LerpSpeedFromGround);

            Quaternion deltaRotation = smoothedRotation * Quaternion.Inverse(Rigidbody.rotation);
            deltaRotation.ToAngleAxis(out float angleDeg, out Vector3 axis);

            if (angleDeg > 180f)
                angleDeg -= 360f;

            float absAngle = Mathf.Abs(angleDeg);
            float t = Mathf.InverseLerp(0, 5, absAngle);
            float filteredAngleRad = Mathf.Sign(angleDeg) * t * absAngle * Mathf.Deg2Rad;

            Vector3 angularVel = axis.normalized * (filteredAngleRad / Time.fixedDeltaTime);
            Vector3 angularExplosionVector = Data.ExplosionAngularVector;

            Rigidbody.angularVelocity = angularVel + angularExplosionVector;
        }

        private void CalculateAdhesion()
        {
            Vector3 normalSum = Vector3.zero;
            int groundedCount = 0;

            for (int i = 0; i < Legs.Length; i++)
            {
                var raycast = Legs[i].Raycast;
                if (!raycast.IsGrounded)
                    continue;

                normalSum += raycast.GroundNormal;
                groundedCount++;
            }

            if (groundedCount == 0)
                return;

            Vector3 averageNormal = (normalSum / groundedCount).normalized;

            float blend = Mathf.Clamp01(groundedCount / 4f);
            averageNormal = Vector3.Slerp(Transform.up, averageNormal, blend);

            Quaternion targetRotation =
                Quaternion.FromToRotation(Transform.up, averageNormal) * Rigidbody.rotation;

            Quaternion smoothedRotation = Quaternion.Slerp(
                Rigidbody.rotation,
                targetRotation,
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