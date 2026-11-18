using Godot;
using System;
using GodotInk;
using Ink.Parsed;

public partial class Dialogue : Control
{
	[Signal] public delegate void ClosedEventHandler();

	[Export] private InkStory story;

	[ExportGroup("Advanced")]
	[Export] private VBoxContainer contentBox;
	[Export] private RichTextLabel content;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (story != null)
			ContinueStory();
	}

	public void Open(InkStory inkStory)
	{
		story = inkStory;
		story.ResetState();
		ContinueStory();
	}

	private void ContinueStory()
	{
		// Remove buttons
		foreach (Node child in contentBox.GetChildren())
		{
			if (child is Button)
				child.QueueFree();
		}

		string newText = story.ContinueMaximally();
		content.Text = string.IsNullOrEmpty(content.Text) ? newText : content.Text + "\n" + newText;

		foreach (InkChoice choice in story.CurrentChoices)
		{
			Button button = new() { Text = choice.Text };
			contentBox.AddChild(button);
			
			button.Pressed += delegate
			{
				story.ChooseChoiceIndex(choice.Index);
				ContinueStory();
			};
		}

		if (story.CurrentChoices.Count == 0)
		{
			Button closeButton = new() { Text = "Close" };
			contentBox.AddChild(closeButton);
			closeButton.Pressed += () => EmitSignal(SignalName.Closed);
		}
	}
}
