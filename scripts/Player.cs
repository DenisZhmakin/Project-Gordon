using Godot;
using static Godot.Mathf;

namespace ProjectGordon.scripts;

public partial class Player : CharacterBody3D
{
	[Export]
	private float _lookSensitivity = 0.006f;
	
	private const float WalkSpeed = 5.0f;
	private const float SprintSpeed = 8.0f;
	private const float JumpVelocity = 4.5f;
	
	private Camera3D _firstPersonCamera;

	public override void _Ready()
	{
		_firstPersonCamera = GetNode<Camera3D>(GetPath()+ "/Head/FirstPersonCamera");
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
		
		var inputDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").Normalized();
		var direction = Transform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y);

		if (IsOnFloor())
		{
			if (Input.IsActionJustPressed("ui_accept"))
			{
				var velocity = Velocity;
				velocity.Y += JumpVelocity;
				Velocity = velocity;
			}
			HandleGroundPhysics(direction);
		}
		else
		{
			HandleAirPhysics(delta);
		}

		MoveAndSlide();
	}

	private void HandleGroundPhysics(Vector3 direction)
	{
		var velocity = Velocity;
		
		velocity.X = direction.X * GetPlayerSpeed();
		velocity.Z = direction.Z * GetPlayerSpeed();
		
		Velocity = velocity;
	}

	private void HandleAirPhysics(double delta)
	{
		var velocity = Velocity;
		velocity.Y -= (float)ProjectSettings.GetSetting("physics/3d/default_gravity") * (float)delta;
		Velocity = velocity;
	}
}