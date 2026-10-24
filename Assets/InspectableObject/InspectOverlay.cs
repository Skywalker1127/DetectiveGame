using Godot;

// Root of InspectOverlay.tscn: a blurred copy of the scene with the inspected object
// rendered on top of it in its own 3D world.
public partial class InspectOverlay : CanvasLayer
{
	[Signal] public delegate void ExitRequestedEventHandler();

	[Export] public Camera3D Camera { get; set; }
	[Export] public MeshInstance3D MoebiusQuad { get; set; }
	[Export] public ColorRect BlurRect { get; set; }
	[Export] public Node3D Pivot { get; set; }
	[Export] public Node3D Holder { get; set; }
	[Export] public Button ExitButton { get; set; }

	public override void _Ready()
	{
		ExitButton.Pressed += () => EmitSignal(SignalName.ExitRequested);
	}

	// Matches the overlay camera to the player's, and reuses the player's Moebius settings.
	public void Setup(Camera3D playerCamera)
	{
		Camera.Fov = playerCamera.Fov;

		// Copy the main screen shader from the player so we keep all the same params, but toggle the mask background parameter on
		var source = Player.PlayerInstance.MoebiusShaderQuad as MeshInstance3D;
		if (source?.MaterialOverlay is ShaderMaterial sourceMaterial)
		{
			var material = (ShaderMaterial)sourceMaterial.Duplicate();
			material.SetShaderParameter("mask_background", true);
			MoebiusQuad.MaterialOverlay = material;
		}
		else
		{
			MoebiusQuad.Visible = false;
		}
	}

	public void SetBlurAmount(float amount)
	{
		((ShaderMaterial)BlurRect.Material).SetShaderParameter("amount", amount);
	}
}
