using Godot;
using GodotInk;

[GlobalClass]
public partial class ReadableDocument : Interactible
{
	[Export] public InkStory Story;

	[ExportGroup("Advanced")]
	[Export] public PackedScene DocumentUi;

	private Document documentInstance;

	public override void Interact()
	{
		if (documentInstance != null)
			return;

		Player.PlayerInstance.bDisablePlayerInput = true;
		Input.SetMouseMode(Input.MouseModeEnum.Visible);

		documentInstance = DocumentUi.Instantiate<Document>();
		AddChild(documentInstance);
		documentInstance.Closed += CloseDocument;
		documentInstance.Open(Story);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (documentInstance != null && @event.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			CloseDocument();
		}
	}

	private void CloseDocument()
	{
		documentInstance.Closed -= CloseDocument;
		documentInstance.QueueFree();
		documentInstance = null;

		Player.PlayerInstance.bDisablePlayerInput = false;
		Input.SetMouseMode(Input.MouseModeEnum.Captured);
	}
}
