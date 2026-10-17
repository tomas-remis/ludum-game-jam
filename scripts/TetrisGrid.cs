using Godot;
using System;

public partial class TetrisGrid : Node2D
{
	private Node2D currentBlock;
	private Node2D nextBlock;
	private int[,] grid = new int[12, 10];
	
	
	private float curCooldown;
	
	[Export]
	private float delay = 0.5f;
	
	[Export]
	private float cellSize = 1.5f;

	[Export]
	private readonly PackedScene[] blocks;
	
	private int thresholdRow = 2;
	private readonly RandomNumberGenerator gen = new();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		curCooldown = delay;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(curCooldown <= 0){
			FallBlock();	
		}else{
			curCooldown -= delay;
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
	}
	
	private void FallBlock(){
		curCooldown = delay;
		foreach(Vector2I coord in currentBlock.gridCoords()){
			int newY = coord.Y + 1;
			
			if(newY > grid.GetLength(0) || grid[newY, coord.Y] == 1){
				PlaceBlock();
				return;
			}
		}
		
		currentBlock.Position += new Vector2(0, cellSize);
		currentBlock.offset.Y += new Vector2I(0, 1);
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
		nextBlock = GetNextBlock();
		
		int offsetY = 0;
		int offsetX = blocks.GetLength(1) / 2;
		foreach(Vector2I coord in currentBlock.coords){
			offsetY = Math.Min(offsetY, coord.Y);
		}

		nextBlock.Position = new Vector2((offsetX + 0.5f) * cellSize, (offsetY + 0.5f) * cellSize);
		nextBlock.offset = new Vector2(offsetX, offsetY);
	}
	
	private void ResetGrid(){
		foreach(Node child in GetChildren()){
			child.QueueFree();
		}

		for(int y = 0; y < grid.GetLength(0); ++y)
		{
			for(int x = 0; x < grid.GetLength(1); ++x)
			{
				grid[y, x] = 0;
			}
		}

	}
	
	private Node2D GetNextBlock(){
		int index = (int) gen.Randi() % blocks.Length;
		return blocks[index].Instantiate() as Node2D;
	}
	
	private void RotateBlock(){
		currentBlock.rotateInternal(true);
		foreach(Vector2I coord in currentBlock.gridCoords())
		{
			if(grid[coord.Y, coord.X] == 1)
			{
				currentBlock.rotateInternal(false);
				return;
			}
		}
		currentBlock.Rotate((float) Math.PI / 2);
	}
	
	private void MoveBlock(bool left){
		int dir = left ? -1 : 1;
		foreach(Vector2I coord in currentBlock.gridCoords()){
			int newX = coord.X + dir;
			
			if(newX < 0 || newX > grid.GetLength(1) || grid[coord.Y, newX] == 1){
				return;
			}
		}
		currentBlock.offset += new Vector2(cellSize * dir, 0);
		currentBlock.Position += new Vector2I(dir, 0);
	}
}
