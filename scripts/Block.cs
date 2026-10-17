using Godot;
using System;
using System.ComponentModel.Design;
using System.Linq;

using static Turn;
public enum Turn { Up, Right, Down, Left }

// TODO: Sprite rendering, rotation and offsetting, scenes for each possible block, generator/enumerator for next block view,
public partial class Block : Sprite2D
{

	public Vector2I[] coords;
	public Vector2I offset = new(0, 0);
	Turn rotation = Up;

	public Vector2I[] gridCoords() => coords.Select(coord => coord + offset);
	public void rotateInternal(bool clockwise)
	{
		if (clockwise)
		{
			coords = coords.Select(coord => new(-coord.y, coord.x));
			rotation = (rotation + 1) % 4;
		}
		else
		{
			coords = coords.Select(coord => new(coord.y, -coord.x));
			rotation = (rotation - 1) % 4;
		}
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
