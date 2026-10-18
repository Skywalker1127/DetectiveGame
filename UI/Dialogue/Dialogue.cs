using Godot;
using System;
using GodotInk;
using Ink.Parsed;

public partial class Dialogue : Control
{
	[Export] private InkStory story;

	[ExportGroup("Advanced")]
	[Export] private VBoxContainer contentBox;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ContinueStory();
	}

	private void ContinueStory()
	{
		foreach (Node child in GetChildren())
			child.QueueFree();
		
		Label content = new() { Text = story.ContinueMaximally() };
		contentBox.AddChild(content);

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
	}
}
