using Godot;
using System;

public partial class TetrisGrid : Node2D
{
	private Node2D currentBlock;
	private Node2D nextBlock;
	private int[,] grid = new int[11, 10];
	
	
	private float curCooldown;
	
	[Export]
	private float delay = 0.5f;
	
	[Export]
	private float cellSize = 1.5f;
	
	private int thresholdRow = 2;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		curCooldown = delay;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(curCooldown <= 0){
			fallBlock();	
		}else{
			curCooldown -= delay;
		}
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("move_left"))
		{
			moveBlock(true);
		}
		else if(@event.IsActionPressed("move_right"))
		{
			moveBlock(false);
		}
		else if(@event.IsActionPressed("rotate")){
			rotateBlock();
		}
	}
	
	private void fallBlock(){
		curCooldown = delay;
		foreach(Vector2I coord in currentBlock.coords){
			int newY = coord.Y + 1;
			
			if(newY > grid.GetLength(0) || grid[newY, coord.Y] == 1){
				placeBlock();
				return;
			}
		}
		
		currentBlock.Position += new Vector2(0, cellSize);
		currentBlock.offset.Y += new Vector2I(0, 1);
	}
	
	private void placeBlock(){
		foreach(Vector2I coord in currentBlock.coords){
			if(coord.Y <= thresholdRow){
				resetGrid();
				return;
			}
			grid[coord.Y, coord.X] = 1;	
		}
		
	}
	
	private void resetGrid(){
		foreach(Node child in GetChildren()){
			child.QueueFree();
		}
		currentBlock = nextBlock; 
	}
	
	private Node2D getNextBlock(){
		
	}
	
	private void rotateBlock(){
		currentBlock.rotateInternal(true);
		foreach(Vector2I coord in currentBlock.coords)
		{
			if(grid[coord.Y, coord.X] == 1)
			{
				currentBlock.rotateInternal(false);
				return;
			}
		}
		currentBlock.Rotate((float) Math.PI / 2);
	}
	
	private void moveBlock(bool left){
		int dir = left ? -1 : 1;
		foreach(Vector2I coord in currentBlock.coords){
			int newX = coord.X + dir;
			
			if(newX < 0 || newX > grid.GetLength(1) || grid[coord.y, newX] == 1){
				return;
			}
		}
		currentBlock.offset += new Vector2(cellSize * dir, 0);
		currentBlock.Position += new Vector2I(dir, 0);
	}
}
