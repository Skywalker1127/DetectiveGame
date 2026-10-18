using Godot;
using GodotInk;

[GlobalClass]
public partial class Character : Interactible
{
	[Export] public InkStory Story;

	[ExportGroup("Advanced")]
	[Export] public PackedScene DialogueUi;

	private Dialogue dialogueInstance;

	public override void Interact()
	{
		if (dialogueInstance != null)
			return;

		Player.PlayerInstance.bDisablePlayerInput = true;
		Input.SetMouseMode(Input.MouseModeEnum.Visible);

		dialogueInstance = DialogueUi.Instantiate<Dialogue>();
		AddChild(dialogueInstance);
		dialogueInstance.Closed += CloseDialogue;
		dialogueInstance.Open(Story);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (dialogueInstance != null && @event.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			CloseDialogue();
		}
	}

	private void CloseDialogue()
	{
		dialogueInstance.Closed -= CloseDialogue;
		dialogueInstance.QueueFree();
		dialogueInstance = null;

		Player.PlayerInstance.bDisablePlayerInput = false;
		Input.SetMouseMode(Input.MouseModeEnum.Captured);
	}
}
