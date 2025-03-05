using System;
using Godot;
using static Godot.Mathf;

namespace ProjectGordon.scripts;

public partial class Player : CharacterBody3D
{
    [Export] private float _lookSensitivity = 0.006f;

    [Export] private float _crouchTranslate = 0.5625f;

    [ExportGroup("Movement Modifiers")]
    [Export] 
    private bool _bunnyHopEnabled;
    [Export] 
    private bool _wallWalkingEnabled;
    
    [ExportGroup("Speed Values")] 
    [Export] 
    private float WalkSpeed { get; set; } = GetVelocityBySpeed(13f);
    [Export] 
    private float SprintSpeed { get; set; } = GetVelocityBySpeed(21.9f);
    [Export] 
    private float CrouchSpeed { get; set; } = GetVelocityBySpeed(4.3f);

    private const float JumpVelocity = 4.5f;

    private const float CameraSmoothnessFactor = 5.0f;
    private const float SpeedIncreasingFactor = 60f;

    private const float HeadbobMoveAmount = 0.06f;
    private const float HeadbobFrequency = 2.4f;

    private bool _isCrouched;
    private float _headbobTime;
    private float _originalShapeHeight;

    private float _airCap = 0.85f;
    private float _airAccel = 800.0f;
    private float _airMoveSpeed = 500.0f;

    private Node3D _cameraContainer;
    private Camera3D _firstPersonCamera;
    private CollisionShape3D _collisionShape;

    private static float GetVelocityBySpeed(float speed)
    {
        return speed / 3.6f;
    }

    public override void _Ready()
    {
        _cameraContainer = GetNode<Node3D>(GetPath() + "/Head/CameraContainer");
        _collisionShape = GetNode<CollisionShape3D>(GetPath() + "/CollisionShape");
        _firstPersonCamera = GetNode<Camera3D>(GetPath() + "/Head/CameraContainer/FirstPersonCamera");

        if (_collisionShape.Shape == null)
        {
            GetTree().Quit();
        }

        _originalShapeHeight = (_collisionShape.Shape as CapsuleShape3D)!.Height;
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

    private float GetPlayerSpeed()
    {
        if (_isCrouched)
        {
            return CrouchSpeed;
        }

        return Input.IsActionPressed("shift") ? SprintSpeed : WalkSpeed;
    }

    public override void _PhysicsProcess(double delta)
    {
        var inputDirection = Input.GetVector(
            "ui_left", "ui_right", "ui_up", "ui_down"
        ).Normalized();
        var direction = Transform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y);

        HandleCrouch(delta);

        if (IsOnFloor())
        {
            if (Input.IsActionJustPressed("ui_accept") || (_bunnyHopEnabled && Input.IsActionPressed("ui_accept")))
            {
                Velocity = new Vector3(Velocity.X, Velocity.Y + JumpVelocity, Velocity.Z);
            }

            HandleGroundPhysics(delta, direction);
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

    private void HandleCrouch(double delta)
    {
        if (Input.IsActionPressed("_crouch"))
        {
            _isCrouched = true;
        }
        else if (_isCrouched && !TestMove(Transform, new Vector3(0, _crouchTranslate, 0)))
        {
            _isCrouched = false;
        }

        // Плавное приседание. 
        _cameraContainer.Position = new Vector3(
            0,
            MoveToward(
                _cameraContainer.Position.Y,
                _isCrouched ? -_crouchTranslate : 0.0f,
                (float)delta * CameraSmoothnessFactor
            ),
            0
        );

        var newShapeHeight = _isCrouched ? _originalShapeHeight - _crouchTranslate : _originalShapeHeight;
        ((CapsuleShape3D)_collisionShape.Shape).Height = newShapeHeight;

        _collisionShape.Position = new Vector3(
            0,
            newShapeHeight / 2,
            0
        );
    }

    private void HandleGroundPhysics(double delta, Vector3 direction)
    {
        // Без SpeedIncreasingFactor скорость передвижения очень маленькая.
        Velocity = new Vector3(
            direction.X * GetPlayerSpeed() * SpeedIncreasingFactor * (float)delta,
            Velocity.Y,
            direction.Z * GetPlayerSpeed() * SpeedIncreasingFactor * (float)delta
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