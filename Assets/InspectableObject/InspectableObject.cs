using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class InspectableObject : Interactible
{
	[Export] public float InspectDistance = 0.6f;
	[Export] public float RotateSensitivity = 0.01f;
	[Export] public float BlendDuration = 0.5f;
	[Export] public float ZoomStep = 0.05f;
	[Export] public float MinInspectDistance = 0.2f;
	[Export] public float MaxInspectDistance = 1.5f;

	private const string OverlayScenePath = "res://Assets/InspectableObject/InspectOverlay.tscn";

	private bool bInspecting = false;
	private Tween tween;
	private float currentDistance;
	private float blend; // 0 = object at rest in the scene, 1 = fully inspected
	private Vector3 localCenter;
	private Vector3 inspectRotation; // euler angles (radians) relative to the camera while inspecting

	// Inspection overlay: the object is shown via a copy rendered in its own world,
	// so it can never clip into (or be hidden by) the level geometry.
	private InspectOverlay overlay;
	private readonly List<Node3D> hiddenOriginals = new();

	public override void Interact()
	{
		if (!bInspecting && overlay == null)
			EnterInspect();
	}

	// Uses _Input so we see events before the Player re-captures the mouse on click.
	public override void _Input(InputEvent @event)
	{
		if (!bInspecting)
			return;

		if (@event.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			ExitInspect();
		}
		else if (@event is InputEventMouseMotion motion && Input.IsActionPressed("interact"))
		{
			GetViewport().SetInputAsHandled();

			// The overlay camera sits at the origin looking down -Z, so screen axes are the world axes.
			inspectRotation.Y += motion.Relative.X * RotateSensitivity;
			inspectRotation.X += motion.Relative.Y * RotateSensitivity;
			UpdateOverlay();
		}
		else if (@event.IsActionPressed("zoom_in") || @event.IsActionPressed("zoom_out"))
		{
			GetViewport().SetInputAsHandled();

			float direction = @event.IsActionPressed("zoom_in") ? -1.0f : 1.0f;
			currentDistance = Mathf.Clamp(currentDistance + direction * ZoomStep, MinInspectDistance, MaxInspectDistance);
			UpdateOverlay();
		}
		else if (@event.IsActionPressed("interact") || @event.IsActionReleased("interact"))
		{
			// Swallow clicks so they don't reach the Player while inspecting, but let the exit button through.
			if (!overlay.ExitButton.GetGlobalRect().HasPoint(GetViewport().GetMousePosition()))
				GetViewport().SetInputAsHandled();
		}
	}

	private void EnterInspect()
	{
		bInspecting = true;
		currentDistance = InspectDistance;

		BuildOverlay();

		tween?.Kill();
		tween = CreateTween().SetTrans(Tween.TransitionType.Cubic);
		tween.TweenMethod(Callable.From<float>(SetBlend), blend, 1.0f, BlendDuration);

		Player.PlayerInstance.bDisablePlayerInput = true;
		Input.SetMouseMode(Input.MouseModeEnum.Visible);
	}

	private void ExitInspect()
	{
		bInspecting = false;
		overlay.ExitButton.Visible = false;

		tween?.Kill();
		tween = CreateTween().SetTrans(Tween.TransitionType.Cubic);
		tween.TweenMethod(Callable.From<float>(SetBlend), blend, 0.0f, BlendDuration);
		tween.TweenCallback(Callable.From(DestroyOverlay));

		Player.PlayerInstance.bDisablePlayerInput = false;
		Input.SetMouseMode(Input.MouseModeEnum.Captured);
	}

	private void SetBlend(float value)
	{
		blend = value;
		UpdateOverlay();
	}

	private void UpdateOverlay()
	{
		overlay.SetBlurAmount(blend);

		// The overlay camera mirrors the player's camera, so the object's real pose in camera space is
		// where the copy has to start and end. Recomputed each frame in case the player moves while closing.
		Camera3D playerCamera = Player.PlayerInstance.Camera;
		Vector3 restPosition = playerCamera.GlobalTransform.AffineInverse() * (GlobalTransform * localCenter);
		Vector3 restRotation = RestRotation(playerCamera);

		Vector3 inspectPosition = new Vector3(0, 0, -currentDistance);
		overlay.Pivot.Position = restPosition.Lerp(inspectPosition, blend);
		overlay.Pivot.Rotation = new Vector3(
			Mathf.LerpAngle(restRotation.X, inspectRotation.X, blend),
			Mathf.LerpAngle(restRotation.Y, inspectRotation.Y, blend),
			Mathf.LerpAngle(restRotation.Z, inspectRotation.Z, blend));
	}

	// The object's rotation as seen from the player's camera, as euler angles.
	private Vector3 RestRotation(Camera3D playerCamera)
	{
		return (playerCamera.GlobalBasis.Inverse() * GlobalBasis).Orthonormalized().GetEuler();
	}

	private void BuildOverlay()
	{
		Camera3D playerCamera = Player.PlayerInstance.Camera;

		overlay = GD.Load<PackedScene>(OverlayScenePath).Instantiate<InspectOverlay>();
		overlay.Setup(playerCamera);
		overlay.ExitRequested += () =>
		{
			if (bInspecting)
				ExitInspect();
		};

		// Start inspecting with the orientation the object has relative to the player's view.
		inspectRotation = RestRotation(playerCamera);
		overlay.Holder.Scale = GlobalBasis.Scale;

		// Copy the nodes from the original inspectable object into the new render scene
		localCenter = ComputeLocalCenter();
		foreach (Node child in GetChildren())
		{
			if (child is Node3D node && child is not CollisionShape3D && child is not CollisionObject3D)
			{
				var copy = (Node3D)node.Duplicate();
				copy.Visible = true;
				copy.Position -= localCenter;
				overlay.Holder.AddChild(copy);

				node.Visible = false;
				hiddenOriginals.Add(node);
			}
		}

		AddChild(overlay);
		blend = 0.0f;
		UpdateOverlay();
	}

	private void DestroyOverlay()
	{
		foreach (Node3D node in hiddenOriginals)
			node.Visible = true;
		hiddenOriginals.Clear();

		overlay?.QueueFree();
		overlay = null;
	}

	// Center of all child meshes' bounds, in this node's local space.
	private Vector3 ComputeLocalCenter()
	{
		Transform3D toLocal = GlobalTransform.AffineInverse();
		bool found = false;
		Aabb bounds = new Aabb();

		foreach (Node child in FindChildren("*", "VisualInstance3D", true, false))
		{
			var visual = (VisualInstance3D)child;
			Aabb local = toLocal * visual.GlobalTransform * visual.GetAabb();
			bounds = found ? bounds.Merge(local) : local;
			found = true;
		}

		return found ? bounds.GetCenter() : Vector3.Zero;
	}
}
