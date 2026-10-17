using Godot;
using System;

public partial class TetrisGrid : Node2D
{
	private Block currentBlock;
	private Block nextBlock;
	private int[,] grid = new int[18, 15];
	
	
	private double curCooldown;
	
	[Export]
	private double delay = 0.5;
	
	[Export]
	public float cellSize = 1.5f;

	[Export]
	private PackedScene[] blocks;

	[Export]
	private Scorer scorer;
	
	private int thresholdRow = 2;
	private readonly RandomNumberGenerator gen = new();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		curCooldown = delay;
		currentBlock = GetNextBlock(0);
		currentBlock.Visible = true;
		nextBlock = GetNextBlock(1);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(curCooldown <= 0){
			FallBlock();	
		}else{
			curCooldown -= delta;
		}
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("move_left"))
		{
			MoveBlock(true);
		}
		else if(@event.IsActionPressed("move_right"))
		{
			MoveBlock(false);
		}
		else if(@event.IsActionPressed("rotate")){
			RotateBlock();
		}
		else if (@event.IsActionPressed("move_down"))
		{
			FallBlock();
		}
	}
	
	private void FallBlock(){
		curCooldown = delay;
		foreach(Vector2I coord in currentBlock.gridCoords()){
			int newY = coord.Y + 1;

			if(newY >= grid.GetLength(0) || grid[newY, coord.X] == 1){
				PlaceBlock();
				return;
			}
		}
		Node2D parent = currentBlock.GetParent<Node2D>();

		parent.Position += new Vector2(0, cellSize);
		currentBlock.offsetInternal += new Vector2I(0, 1);
	}
	
	private void PlaceBlock(){
		foreach(Vector2I coord in currentBlock.gridCoords()){
			if(coord.Y <= thresholdRow){
				ResetGrid();
				break;
			}
			grid[coord.Y, coord.X] = 1;	
		}
		currentBlock = nextBlock;
		currentBlock.Visible = true;
		nextBlock = GetNextBlock();
	}
	
	private void ResetGrid(){
		foreach(Node child in GetChildren()){
			if(nextBlock != child)
				child.QueueFree();
		}

		int score = 0;

		for(int y = 0; y < grid.GetLength(0); ++y)
		{
			for(int x = 0; x < grid.GetLength(1); ++x)
			{
				if(grid[y, x] != 0)
				{
					score += (y < thresholdRow) ? -50 : 50;
				}
				grid[y, x] = 0;
			}
		}
		scorer.UpdateScore(score);
	}
	
	private Block GetNextBlock(int index=0){
		//int index = gen.RandiRange(0, blocks.Length - 1);
		//int index = 1;
		Node2D parent = blocks[index].Instantiate() as Node2D;
		Block block = parent.FindChild("Sprite", recursive: true, owned: false) as Block;
		//block.Visible = false;

		int offsetInternalY = 0;
		int offsetInternalX = grid.GetLength(1) / 2;
		foreach(Vector2I coord in block.coords){
			offsetInternalY = Math.Min(offsetInternalY, coord.Y);
		}
		block.offsetInternal = new Vector2I(offsetInternalX, offsetInternalY);
		block.AdjustScale(cellSize);

		parent.Position = new Vector2(
			offsetInternalX + block.pivot.X,
			offsetInternalY + block.pivot.Y
		) * cellSize;
 
		AddChild(parent);
		return block;
	}
	
	private void RotateBlock(){
		currentBlock.rotateInternal(true);
		foreach(Vector2I coord in currentBlock.gridCoords())
		{
			if(coord.X < 0 || coord.Y < 0 || coord.X >= grid.GetLength(1) || coord.Y >= grid.GetLength(0) || grid[coord.Y, coord.X] == 1)
			{
				currentBlock.rotateInternal(false);
				return;
			}
		}
		Node2D parent = currentBlock.GetParent<Node2D>();
		parent.Rotate(Mathf.Pi / 2);
	}
	
	private void MoveBlock(bool left){
		int dir = left ? -1 : 1;
		foreach(Vector2I coord in currentBlock.gridCoords()){
			int newX = coord.X + dir;

			GD.Print(newX);
			
			if(newX < 0 || newX >= grid.GetLength(1) || grid[coord.Y, newX] == 1){
				return;
			}
		}
		Node2D parent = currentBlock.GetParent<Node2D>();
		parent.Position += new Vector2(cellSize * dir, 0);
		currentBlock.offsetInternal += new Vector2I(dir, 0);
	}
}
