using Godot;
using System;

public partial class PiecePreviewer : TextureRect
{

	private Sprite2D previewSprite;

	public void UpdateSprite(Texture2D newTex)
	{
		Texture = newTex;
	}
}
