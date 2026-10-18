using Godot;
using System;

public partial class Scorer : Label
{
	// Called when the node enters the scene tree for the first time.
	private int score = 0;
	public override void _Ready()
	{
		Text = "Score   0";
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}

	public void UpdateScore(int addedScore)
	{
		score += addedScore;
		Text = "Score     " + addedScore.ToString();
	}
}
