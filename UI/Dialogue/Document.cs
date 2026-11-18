using Godot;
using System.Collections.Generic;
using GodotInk;

public partial class Document : Control
{
	[Signal] public delegate void ClosedEventHandler();

	[Export] private InkStory story;

	[ExportGroup("Advanced")]
	[Export] private RichTextLabel content;
	[Export] private Button previousButton;
	[Export] private Button nextButton;
	[Export] private Button closeButton;
	[Export] private Label pageLabel;

	private readonly List<string> pages = new();
	private string fullText;
	private Vector2 paginatedSize;
	private int pageIndex;

	public override void _Ready()
	{
		previousButton.Pressed += () => ShowPage(pageIndex - 1);
		nextButton.Pressed += () => ShowPage(pageIndex + 1);
		closeButton.Pressed += () => EmitSignal(SignalName.Closed);

		// Layout may not have run yet, so paginate whenever the label gets (or changes) its real size.
		content.Resized += Paginate;

		if (story != null)
			Open(story);
	}

	public void Open(InkStory inkStory)
	{
		story = inkStory;
		story.ResetState();
		fullText = story.ContinueMaximally();

		paginatedSize = Vector2.Zero;
		Paginate();
	}

	private void Paginate()
	{
		if (fullText == null || content.Size.Y <= 0 || content.Size == paginatedSize)
			return;

		paginatedSize = content.Size;
		BuildPages();
		ShowPage(pageIndex);
	}

	// Adds words to the label until its content no longer fits, then starts a new page.
	private void BuildPages()
	{
		pages.Clear();
		List<string> pageWords = new();

		foreach (string word in fullText.Split(' '))
		{
			pageWords.Add(word);
			content.Text = string.Join(' ', pageWords);

			if (pageWords.Count > 1 && content.GetContentHeight() > content.Size.Y)
			{
				pageWords.RemoveAt(pageWords.Count - 1);
				pages.Add(string.Join(' ', pageWords));
				pageWords.Clear();
				pageWords.Add(word);
			}
		}

		pages.Add(string.Join(' ', pageWords));
	}

	private void ShowPage(int index)
	{
		pageIndex = Mathf.Clamp(index, 0, pages.Count - 1);
		content.Text = pages[pageIndex];

		previousButton.Disabled = pageIndex == 0;
		nextButton.Disabled = pageIndex == pages.Count - 1;
		pageLabel.Text = $"{pageIndex + 1} / {pages.Count}";
	}
}
