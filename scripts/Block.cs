using Godot;
using System;
using System.ComponentModel.Design;

enum Rotation
{
	Up,
	Right,
	Down,
	Left
}
public partial class Block : Godot.Sprite2D
{
	public readonly Vector2I[] coords;
	Vector2I offset = new(0, 0);
	Rotation rotation = Up;


	public Vector2I[] gridCoords() => coords.Select(coord => coord + offset);
	public void rotateInternal(bool clockwise)
	{
		if (clockwise) coords = coords.Select(coord => new(-coord.y, coord.x));
		else coords = coords.Select(coord => new(coord.y, -coord.x));
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
