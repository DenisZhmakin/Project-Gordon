using Godot;
using static Godot.Mathf;

namespace ProjectGordon.scripts;

public partial class Player : CharacterBody3D
{
    [Export] private float _lookSensitivity = 0.006f;

    [Export] private bool _isEnableBunnyHop;

    private const float WalkSpeed = 5.0f;
    private const float SprintSpeed = 8.0f;
    private const float JumpVelocity = 5f;

    private const float HeadbobMoveAmount = 0.06f;
    private const float HeadbobFrequency = 2.4f;

    private float _headbobTime;
    private float _airCap = 0.85f;
    private float _airAccel = 800.0f;
    private float _airMoveSpeed = 500.0f;
    private Camera3D _firstPersonCamera;

    public override void _Ready()
    {
        _firstPersonCamera = GetNode<Camera3D>(GetPath() + "/Head/FirstPersonCamera");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton)
        {
            Input.SetMouseMode(Input.MouseModeEnum.Captured);
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            Input.SetMouseMode(Input.MouseModeEnum.Visible);
        }
        else if (Input.MouseMode == Input.MouseModeEnum.Captured && @event is InputEventMouseMotion eventMouseMotion)
        {
            RotateY(-eventMouseMotion.Relative.X * _lookSensitivity);
            _firstPersonCamera.RotateX(-eventMouseMotion.Relative.Y * _lookSensitivity);

            var rotation = _firstPersonCamera.Rotation;
            rotation.X = Clamp(rotation.X, DegToRad(-60), DegToRad(60));
            _firstPersonCamera.Rotation = rotation;
        }
    }

    private static float GetPlayerSpeed()
    {
        return Input.IsActionPressed("shift") ? SprintSpeed : WalkSpeed;
    }

    public override void _PhysicsProcess(double delta)
    {
        var inputDirection = Input.GetVector(
            "ui_left", "ui_right", "ui_up", "ui_down"
        ).Normalized();
        var direction = Transform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y);

        if (IsOnFloor())
        {
            if (Input.IsActionJustPressed("ui_accept") || (_isEnableBunnyHop && Input.IsActionPressed("ui_accept")))
            {
                Velocity = new Vector3(Velocity.X, Velocity.Y + JumpVelocity, Velocity.Z);
            }

            HandleGroundPhysics(direction);
            HeadbobEffect(delta);
        }
        else
        {
            Velocity = new Vector3(
                Velocity.X,
                Velocity.Y - (float)ProjectSettings.GetSetting("physics/3d/default_gravity") * (float)delta,
                Velocity.Z
            );
            HandleAirAcceleration(delta, direction);
        }

        MoveAndSlide();
    }

    private void HeadbobEffect(double delta)
    {
        _headbobTime += (float)delta * Velocity.Length();

        var fpCameraTransform = _firstPersonCamera.Transform;
        fpCameraTransform.Origin = new Vector3(
            Cos(_headbobTime * HeadbobFrequency * 0.5f) * HeadbobMoveAmount,
            Sin(_headbobTime * HeadbobFrequency) * HeadbobMoveAmount,
            0
        );
        _firstPersonCamera.Transform = fpCameraTransform;
    }

    private void HandleGroundPhysics(Vector3 direction)
    {
        Velocity = new Vector3(
            direction.X * GetPlayerSpeed(), Velocity.Y, direction.Z * GetPlayerSpeed()
        );
    }

    private void HandleAirAcceleration(double delta, Vector3 direction)
    {
        var currentSpeed = Velocity.Dot(direction);
        var cappedSpeed = Min((_airMoveSpeed * direction).Length(), _airCap);
        var addSpeedLimit = cappedSpeed - currentSpeed;
        if (addSpeedLimit <= 0) return;
        var accelSpeed = _airAccel * _airMoveSpeed * (float)delta;
        accelSpeed = Min(accelSpeed, addSpeedLimit);
        Velocity += direction * accelSpeed;
    }
}