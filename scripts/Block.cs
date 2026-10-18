using Godot;
using Godot.NativeInterop;
using System;
using System.ComponentModel.Design;
using System.Drawing;
using System.Linq;

using static Turn;
public enum Turn { Up, Right, Down, Left }

// TODO: Sprite rendering, rotation and offsetInternalting, scenes for each possible block, generator/enumerator for next block view,
public partial class Block : Sprite2D
{

	[Export]
	public Godot.Collections.Array<Vector2I> coords;

	[Export]
	public Vector2 pivot;
	[Export]
	public Vector2I offsetInternal = new(0, 0);
	Turn rotation = Up;

	public override void _Ready()
	{
		var rect = new ColorRect
			{
				Color = Colors.Blue,
				Size = new Vector2(30, 30) / Scale,
				Position = new Vector2(-15, -15)
			};
		AddChild(rect);		
	}


	public void AdjustScale(float cellSize)
	{
		int minX = coords.Min(v => v.X);	
		int minY = coords.Min(v => v.Y);
		int maxX = coords.Max(v => v.X);	
		int maxY = coords.Max(v => v.Y);
		Vector2 textSize = Texture.GetSize();
		float xScale = cellSize * (maxX - minX + 1) / textSize.X;
		float yScale = cellSize * (maxY - minY + 1) / textSize.Y;
		Scale = new Vector2(xScale, yScale);

		Node2D pivotNode = GetParent<Node2D>();
		
		//pivotNode.Position = pivot * cellSize;
		Position = -pivot * cellSize;
		/**
		foreach(Vector2I coord in coords)
		{
			Vector2 size = new Vector2(cellSize, cellSize) / Scale;
			var rect = new ColorRect
			{
				Color = Colors.Red,
				Size = size,
				Position = new Vector2(coord.X, coord.Y) * cellSize / Scale
			};
			AddChild(rect);
		}

		Node2D parent = GetParent<Node2D>();
		ColorRect rectp = new ColorRect
			{
				Color = Colors.Green,
				Size = new Vector2(30, 30) / Scale,
				Position = new Vector2(-15, -15)
			};
		parent.AddChild(rectp);
		**/
	}


	public Godot.Collections.Array<Vector2I> gridCoords() => [.. coords.Select(coord => coord + offsetInternal)];

	public Vector2 rotateInternal()
	{
		var corners = coords.Select(coord =>
		{
			Vector2 d = new Vector2(coord.X + 0.5f, coord.Y + 0.5f) - pivot;
			d = new Vector2(-d.Y, d.X);   // rotate 90° clockwise
			return pivot + d - new Vector2(0.5f, 0.5f);
		}).ToArray();

		Vector2 delta = new Vector2(
			Mathf.Round(corners[0].X) - corners[0].X,
			Mathf.Round(corners[0].Y) - corners[0].Y);

		coords = [.. corners.Select(c => new Vector2I(
			Mathf.RoundToInt(c.X + delta.X),
			Mathf.RoundToInt(c.Y + delta.Y)))];

		pivot += delta;
		return delta;
	}
	// block shape presets

	public static readonly Vector2I[] one = [new(0, 0)];
	public static readonly Vector2I[] square = [new(0, 0), new(1, 0), new(0, 1), new(1, 1)];
	// public static readonly Vector2I[] i = [new(0, 1), new(1, 1), new(2, 1), new(3, 1)];
	// public static readonly Vector2I[] t = [new(1, 0), new(0, 1), new(1, 1), new(2, 1)];
	// public static readonly Vector2I[] z = [new(0, 0), new(1, 0), new(1, 1), new(2, 1)];
	// public static readonly Vector2I[] s = [new(1, 0), new(2, 0), new(0, 1), new(1, 1)];
	// public static readonly Vector2I[] l = [new(2, 0), new(0, 1), new(1, 1), new(2, 1)];
	// public static readonly Vector2I[] j = [new(0, 0), new(0, 1), new(1, 1), new(2, 1)];

	// public static readonly Vector2I[][] shapes = [i, t, o, z, s, l, j];
}
