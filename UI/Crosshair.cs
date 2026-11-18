using Godot;

public partial class Crosshair : TextureRect
{
	[Export] public Texture2D DefaultTexture { get; set; }
	[Export] public Texture2D InteractibleTexture { get; set; }

	public override void _Process(double delta)
	{
		var player = Player.PlayerInstance;
		bool lookingAtInteractible = player != null && !player.bDisablePlayerInput
			&& player.GetLookedAtInteractible() != null;

		Texture = lookingAtInteractible ? InteractibleTexture : DefaultTexture;
	}
}
