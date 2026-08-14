using Infastructure.Common.StableWorlUpManagement;
using Infastructure.Services.CursorVisible;
using Infastructure.Services.Defeat;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerInput.InputSourceRealization;
using Infastructure.Services.Registries.SpiderRegistry;
using Infastructure.Services.Window;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using SpiderController;
using SpiderController.StateMachine;
using UnityEngine;

namespace Cameras.SpiderCameras
{
    public class SpiderCameraOrbit
    {
        private readonly IInputService _inputService;
        private readonly IStaticDataService _staticDataService;
        private readonly IStableWorldUp _stableWorldUp;
        private readonly IDefeatWindowService _defeatWindowService;
        private readonly IWindowService _windowService;
        private readonly ICursorVisibleService _cursorVisibleService;
        private readonly ISpiderRegistryService _spiderRegistryService;
        private readonly ISpiderCamera _spiderCamera;
        private readonly Transform _pivot;

        private Spider Spider => _spiderRegistryService.Spider;
        private StateMachineData Data => _spiderRegistryService.Spider.StateMachineData;

        /// <summary>
        /// The middle anchor of the orbit: the pose authored in the prefab's ShoulderOffset, shifted
        /// by config. Everything that reasons about the neutral angle must go through here — the arc
        /// and the pitch clamp both do, because using the raw authored angle for one and the shifted
        /// one for the other moves the reachable range instead of the pose.
        /// </summary>
        private float NeutralPitch =>
            _neutralPitch + _staticDataService.SpiderStaticData.CameraNeutralAngleOffset;

        private JoystickInputSource _joystickInputSource;

        private float _cameraRotationSpeed;
        private float _defaultShoulderY;

        private bool _isMouseRotating;
        private bool _centerMouseHolding;

        // Lift of the orbit's centre above the spider — 0 at rest, raised only by the climb
        // compensation. Distinct from the shoulder height: that lived at the authored offset and
        // fed the arc's centre directly, which is the bug this ticket fixes.
        private float _climbLift;
        private float _xRotation;
        private float _xRotationAiming;

        private Quaternion _orbitStartRotation;
        private Quaternion _orbitStartRotationAiming;

        // World up as of the last frame, so CarryOrbitStartWithWorldUp can tell how far it turned
        // this frame and carry the orbit's yaw reference by exactly that much.
        private Vector3 _lastWorldUp;

        // Pitch never touches the pivot's rotation: CinemachineThirdPersonFollow strips pitch out
        // of the follow target (GetHeading projects onto the world-up plane) and only honours it
        // through VerticalArmLength, which is 0 here. So the vertical orbit is built as a shoulder
        // offset instead — see CameraPitchMath.ShoulderArc.
        private float _pitch;
        private float _appliedPitch;

        private float _shoulderX;
        private float _orbitRadius;
        private float _neutralPitch;

        public SpiderCameraOrbit(
            IInputService inputService,
            IStaticDataService staticDataService,
            IStableWorldUp stableWorldUp,
            IDefeatWindowService defeatWindowService,
            IWindowService windowService,
            ICursorVisibleService cursorVisibleService,
            ISpiderRegistryService spiderRegistryService,
            ISpiderCamera spiderCamera,
            Transform pivot)
        {
            _inputService = inputService;
            _staticDataService = staticDataService;
            _stableWorldUp = stableWorldUp;
            _defeatWindowService = defeatWindowService;
            _windowService = windowService;
            _cursorVisibleService = cursorVisibleService;
            _spiderRegistryService = spiderRegistryService;
            _spiderCamera = spiderCamera;
            _pivot = pivot;
        }


        public void Initialize()
        {
            StartInput();

            Vector3 authoredShoulder = _spiderCamera.ShoulderOffset;
            _shoulderX = authoredShoulder.x;
            _defaultShoulderY = authoredShoulder.y;

            // Orbit is centred on the spider itself, not on the authored shoulder position —
            // radius and neutral angle are derived from the authored offset so that zero pitch
            // reproduces it exactly. See CameraPitchMath.ShoulderArc.
            _orbitRadius = CameraPitchMath.OrbitRadius(authoredShoulder.y, authoredShoulder.z);
            _neutralPitch = CameraPitchMath.NeutralOrbitAngle(authoredShoulder.y, authoredShoulder.z);

            _cameraRotationSpeed = _staticDataService.SpiderStaticData.CameraRotationSpeed;

            _windowService.OnWindowOpened += ReleaseInput;
            _inputService.OnJoystickEnableHappend += JoystickEnabled;
            _inputService.OnJoystickDisableHappend += JoystickDisabled;
            _cursorVisibleService.OnHideCursorHappened += StartInput;
        }

        public void Destroy()
        {
            _windowService.OnWindowOpened -= ReleaseInput;
            _inputService.OnJoystickEnableHappend -= JoystickEnabled;
            _inputService.OnJoystickDisableHappend -= JoystickDisabled;
            _cursorVisibleService.OnHideCursorHappened -= StartInput;
        }

        public void Update()
        {
            if (Spider == null || _defeatWindowService.IsDefeated)
                return;

            CarryOrbitStartWithWorldUp();

            CameraCalculateHandle();

            if (_isMouseRotating)
            {
                if (_centerMouseHolding)
                    RotateCameraAiming();
                else
                    RotateCamera();
            }

            HandleMouse();
        }

        /// <summary>
        /// StableWorldUp now holds the camera horizon steady through small tilts (a rock, a step)
        /// and only turns it on a sustained wall/ceiling transition — see StableWorldUp.Rotate.
        /// When it does turn, the orbit's yaw reference has to turn with it, or the player's
        /// accumulated _xRotation would suddenly be measured against a different plane, which is
        /// exactly the "camera snaps to face the wrong way" bug this replaces. Carrying both stored
        /// rotations by the same incremental delta keeps the player's relative look direction —
        /// same shoulder, same side of the spider — through the whole transition, with no
        /// projection to degenerate at 90°.
        /// </summary>
        private void CarryOrbitStartWithWorldUp()
        {
            if (_stableWorldUp.StableWorldUpTransform == null)
                return;

            Vector3 currentWorldUp = _stableWorldUp.StableWorldUpTransform.up;
            if (currentWorldUp == _lastWorldUp)
                return;

            Quaternion delta = Quaternion.FromToRotation(_lastWorldUp, currentWorldUp);
            _orbitStartRotation = delta * _orbitStartRotation;
            _orbitStartRotationAiming = delta * _orbitStartRotationAiming;
            _lastWorldUp = currentWorldUp;
        }

        /// <summary>
        /// Single writer for ShoulderOffset: the climb compensation supplies the base height,
        /// the player's pitch swings the camera around the spider on top of it. Composing them in
        /// one place is what keeps the two from fighting over the same value.
        /// </summary>
        private void ApplyShoulderOffset()
        {
            SpiderStaticData data = _staticDataService.SpiderStaticData;

            _appliedPitch = Mathf.Lerp(_appliedPitch, _pitch, _cameraRotationSpeed * Time.deltaTime);

            float neutral = NeutralPitch;
            float orbitAngle = neutral + _appliedPitch;

            // Bottom/top of the reachable range in the same absolute-angle frame InterpolateByAngle
            // now works in — orbitAngle already lives there, so no travel conversion needed for the
            // three params below. downTravel is kept only for FramingScreenOffset, which still
            // measures from zero pitch, not from an absolute angle.
            float bottomAngle = -data.MaxPitchUpAngle;
            float topAngle = data.MaxPitchDownAngle;
            float downTravel = data.MaxPitchDownAngle - neutral;

            // Scale, not an absolute distance: 1 keeps the radius the artist authored in the prefab,
            // so the middle anchor at 1 still reproduces the authored offset exactly and the whole
            // thing survives someone re-authoring ShoulderOffset. Note this deliberately gives up
            // the constant-radius guarantee ticket 05 introduced — the spider's on-screen size will
            // change across the pitch range again, which is the point of having a zoom at all.
            float radiusScale = CameraPitchMath.InterpolateByAngle(
                orbitAngle,
                bottomAngle, data.CameraOrbitRadiusScaleBottom,
                neutral, data.CameraOrbitRadiusScaleMiddle,
                data.CameraSteepAngle, data.CameraOrbitRadiusScaleSteep,
                topAngle, data.CameraOrbitRadiusScaleTop);

            // Both inputs are smoothed state (_climbLift by ClimbMoveCamera, _appliedPitch above),
            // so this reads nothing back from ShoulderOffset. That matters: ShoulderOffset is now a
            // computed value containing the arc, and lerping towards it from itself compounds the
            // arc every frame — which sends the camera into orbit within a second.
            _spiderCamera.ShoulderOffset = CameraPitchMath.ShoulderArc(
                _shoulderX, _climbLift, _orbitRadius * radiusScale, orbitAngle);

            _spiderCamera.FramingVerticalOffset = CameraPitchMath.FramingScreenOffset(
                _appliedPitch, downTravel, data.PitchScreenOffset);

            // Read every frame like the framing offset above, not just once in Initialize — so it
            // can be tuned live in Play Mode instead of requiring a restart per value.
            //
            // Two axes rather than one because they trade places across the orbit: up is
            // perpendicular to the view down low and along it up high, forward the reverse. Height
            // alone left the overhead shot with no framing at all.
            _spiderCamera.AimHeight = CameraPitchMath.InterpolateByAngle(
                orbitAngle,
                bottomAngle, data.CameraAimHeightBottom,
                neutral, data.CameraAimHeightMiddle,
                data.CameraSteepAngle, data.CameraAimHeightSteep,
                topAngle, data.CameraAimHeightTop);

            _spiderCamera.AimForward = CameraPitchMath.InterpolateByAngle(
                orbitAngle,
                bottomAngle, data.CameraAimForwardBottom,
                neutral, data.CameraAimForwardMiddle,
                data.CameraSteepAngle, data.CameraAimForwardSteep,
                topAngle, data.CameraAimForwardTop);
        }

        public void AlignToSpider()
        {
            if (_stableWorldUp.StableWorldUpTransform == null)
                return;

            Vector3 worldUp = _stableWorldUp.StableWorldUpTransform.up;
            Vector3 forward = Vector3.ProjectOnPlane(Spider.transform.forward, worldUp).normalized;

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.ProjectOnPlane(Spider.transform.right, worldUp).normalized;

            _orbitStartRotation = Quaternion.LookRotation(forward, worldUp);
            _pivot.rotation = _orbitStartRotation;
            _xRotation = 0f;
            _lastWorldUp = worldUp;
        }

        private void CameraCalculateHandle()
        {
            if (!_centerMouseHolding)
            {
                if (_isMouseRotating)
                    CalculateMoveCamera();
            }
            else
            {
                _xRotationAiming += _inputService.MouseXAxis * _staticDataService.SpiderStaticData.MouseRotationSpeedX;
            }

            // Climb compensation now runs every frame rather than only when the player isn't
            // rotating. It used to share ShoulderOffset.y with the mouse, so the two had to take
            // turns; the mouse drives the pitch angle now, leaving the height to the climb alone.
            ClimbMoveCamera();
            ApplyShoulderOffset();
        }

        private void RotateCamera()
        {
            Vector3 up = _stableWorldUp.StableWorldUpTransform.up;
            Quaternion targetRot = Quaternion.AngleAxis(_xRotation, up) * _orbitStartRotation;

            _pivot.rotation = Quaternion.Slerp(
                _pivot.rotation,
                targetRot,
                Time.deltaTime * _cameraRotationSpeed);
        }

        private void RotateCameraAiming()
        {
            Vector3 up = _stableWorldUp.StableWorldUpTransform.up;
            Quaternion targetRot = Quaternion.AngleAxis(_xRotationAiming, up) * _orbitStartRotationAiming;

            _pivot.rotation = Quaternion.Slerp(
                _pivot.rotation,
                targetRot,
                Time.deltaTime * _cameraRotationSpeed);
        }

        // Unchanged behaviour, minus the write: it still computes the same climb target height into
        // _climbLift, but ApplyShoulderOffset is now the only thing that touches ShoulderOffset.
        // Target is expressed as lift above the orbit centre (0 at rest) rather than an absolute
        // shoulder height, so the ClimbShoulderMaxY setting keeps producing the same amount of
        // travel (ClimbShoulderMaxY - _defaultShoulderY) it always did.
        private void ClimbMoveCamera()
        {
            // Tilt is the angle between the spider's own up and world up, not the Z euler component.
            // A euler component only captures the tilt when the spider happens to be facing a
            // particular way — climb on a slope entered heading along world X lands in the X
            // component instead and read as zero. That's why the raise appeared on stairs and some
            // slopes and silently did nothing on others: it depended on heading, not on steepness.
            SpiderStaticData data = _staticDataService.SpiderStaticData;

            float spiderTilt = Vector3.Angle(Spider.transform.up, Vector3.up);
            float targetLift = Mathf.Lerp(
                0f,
                data.ClimbShoulderMaxY - _defaultShoulderY,
                Mathf.Clamp01(Mathf.InverseLerp(0f, data.ClimbTiltMaxAngle, spiderTilt)));

            _climbLift = Mathf.Lerp(_climbLift, targetLift, _cameraRotationSpeed * Time.deltaTime);
        }

        private void CalculateMoveCamera()
        {
            SpiderStaticData data = _staticDataService.SpiderStaticData;

            _xRotation += _inputService.MouseXAxis * data.MouseRotationSpeedX;

            // Same sign as before (mouse forward → look up): this used to slide the shoulder
            // offset between 0 and 3 units, now it feeds a pitch angle in degrees that
            // ApplyShoulderOffset turns into an arc.
            _pitch -= _inputService.MouseYAxis * data.PitchSensitivity;
            _pitch = CameraPitchMath.ClampPitch(_pitch, NeutralPitch, data.MaxPitchDownAngle, data.MaxPitchUpAngle);
        }

        private void HandleMouse()
        {
            if (Spider == null || Data.IsGravityGunState)
                return;

            if (_inputService.CenterMousePressed)
            {
                _centerMouseHolding = true;
                _orbitStartRotationAiming = _pivot.rotation;
                _xRotationAiming = 0f;
            }

            if (_inputService.CenterMouseUp)
            {
                StartInput();
                _centerMouseHolding = false;
            }

            if (_inputService.LeftMousePressed)
                ReleaseInput();

            if (_inputService.LeftMouseUp)
                StartInput();
        }

        private void ReleaseInput() =>
            _isMouseRotating = false;

        private void StartInput()
        {
            _xRotation = 0;
            _isMouseRotating = true;

            if (_stableWorldUp.StableWorldUpTransform == null)
                return;

            Vector3 worldUp = _stableWorldUp.StableWorldUpTransform.up;
            Vector3 forward = Vector3.ProjectOnPlane(_pivot.forward, worldUp);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.Cross(worldUp, _pivot.right);

            forward.Normalize();

            _orbitStartRotation = Quaternion.LookRotation(forward, worldUp);
            _pivot.rotation = _orbitStartRotation;
            _lastWorldUp = worldUp;
        }

        private void JoystickEnabled(IInputSource obj) =>
            _joystickInputSource = (JoystickInputSource)obj;

        private void JoystickDisabled() =>
            _joystickInputSource = null;
    }
}