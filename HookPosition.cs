using Godot;
using System;
using System.Globalization;

public partial class HookPosition : Node2D
{
	private Sprite2D hook;
	private Sprite2D wire;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		hook = GetChild<Sprite2D>(0);
		wire = GetChild<Sprite2D>(1);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public void UpdateHookPosition(Node2D parent)
	{
		Vector2 lastPos = parent is null ? -Vector2.One : parent.GlobalPosition + Vector2.Up * 50;

		hook.GlobalPosition = lastPos;
		var r = wire.RegionRect;
		wire.GlobalPosition = new Vector2(lastPos.X - 32, wire.GlobalPosition.Y);
		wire.RegionRect = new Rect2(0, 0, r.Size.X, lastPos.Y - wire.GlobalPosition.Y);
	}
}
