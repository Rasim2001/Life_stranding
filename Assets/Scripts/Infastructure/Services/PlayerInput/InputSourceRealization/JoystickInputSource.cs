using Infastructure.Services.PlayerInput.InputSourceRealization;
using UnityEngine;
using UnityEngine.InputSystem;

public class JoystickInputSource : IInputSource
{
    public bool IsRotationButtonPressed { get; private set; }

    private const float Sensitivity = 1f;

    private GameInput _gi;
    private InputAction Move => _gi.Joystick.Movement;
    private InputAction Jump => _gi.Joystick.Jump;
    private InputAction Magnet => _gi.Joystick.Magnet;
    private InputAction Jerk => _gi.Joystick.Jerk;
    private InputAction Scan => _gi.Joystick.Scan;
    private InputAction Pickup => _gi.Joystick.Pickup;
    private InputAction Sitdown => _gi.Joystick.Sitdown;
    private InputAction LookBtn => _gi.Joystick.LookButton;
    private InputAction Shift => _gi.Joystick.ShiftMove;
    private InputAction Scroll => _gi.Joystick.ScrollCamera;
    private InputAction PlaneRot => _gi.Joystick.PlaneRotation;
    private InputAction ChangePlatformX => _gi.Joystick.ChangePlatformLeft;
    private InputAction ChangePlatformB => _gi.Joystick.ChangePlatformRight;
    private InputAction Pause => _gi.Joystick.Pause;

    private float _scroll;

    public void Enable()
    {
        _gi = new GameInput();
        _gi.Enable();
    }

    public void Disable()
    {
        _gi?.Disable();
        _gi = null;
    }

    public bool ChangePlatformLeftPressed => ChangePlatformX.WasPressedThisFrame();
    public bool ChangePlatformRightPressed => ChangePlatformB.WasPressedThisFrame();
    public bool PauseButtonPressed => Pause.WasPressedThisFrame();

    public Vector3 InputVector =>
        new Vector3(Move.ReadValue<Vector2>().x, 0f, Move.ReadValue<Vector2>().y);

    public bool JumpPressed => Jump.WasPressedThisFrame();
    public bool JumpUp => Jump.WasReleasedThisFrame();

    public bool JerkPressed => Jerk.WasPressedThisFrame();

    public bool CenterMousePressed
    {
        get
        {
            bool isPressed = LookBtn.WasPressedThisFrame();

            if (isPressed)
                IsRotationButtonPressed = true;

            return isPressed;
        }
    }
    public bool CenterMouseUp
    {
        get
        {
            bool isPressedUp = LookBtn.WasReleasedThisFrame();

            if (isPressedUp)
                IsRotationButtonPressed = false;

            return isPressedUp;
        }
    }

    public bool CtrlPressed => Sitdown.WasPressedThisFrame();
    public bool CtrlUp => Sitdown.WasReleasedThisFrame();

    public bool IsLeftShiftPressed => Shift.WasPressedThisFrame();
    public bool IsLeftShiftUp => Shift.WasReleasedThisFrame();

    public bool PickupPressed => Pickup.WasPressedThisFrame();
    public bool TabPressed => Scan.WasPressedThisFrame();

    public bool AnyKeyPressed() =>
        false;

    //public bool PauseButtonPressed => Pause.WasPressedThisFrame();

    public float ScrollWheelAxis
    {
        get
        {
            float raw = Scroll.ReadValue<float>();
            if (Mathf.Abs(raw) > 0.001f)
                _scroll = Mathf.MoveTowards(_scroll, Mathf.Sign(raw), 0.001f);
            else
                _scroll = Mathf.MoveTowards(_scroll, 0f, 0.001f);

            return _scroll;
        }
    }
    public float MouseXAxis => PlaneRot.ReadValue<Vector2>().x;
    public float MouseYAxis => PlaneRot.ReadValue<Vector2>().y;

    public bool IsGamepadActiveNow()
        => _gi != null && (
            Move.ReadValue<Vector2>().sqrMagnitude > 0.01f ||
            PlaneRot.ReadValue<Vector2>().sqrMagnitude > 0.01f ||
            Jump.triggered ||
            Jerk.triggered ||
            Pickup.triggered ||
            Scan.triggered
        );

    public bool LeftMousePressed => false;
    public bool LeftMouseUp => false;
    public bool RightMousePressed => Magnet.WasPressedThisFrame();
    public bool RightMouseUp => Magnet.WasReleasedThisFrame();
}