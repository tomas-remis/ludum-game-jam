using Godot;
using System;

public partial class TetrisGrid : Node2D
{
	[Export]
	private Sprite2D wipeSprite;

	[Export]
	private double wipeDuration = 0.8;
	private bool wiping;



	private Block currentBlock;
	private Block nextBlock;
	private int[,] grid = new int[18, 15];

	[Export]
	private AudioStreamPlayer placeSound;
	
	
	private double curCooldown;
	
	[Export]
	private double delay = 0.5;
	
	[Export]
	public float cellSize = 1.5f;

	[Export]
	private PackedScene[] blocks;

	[Export]
	private Scorer scorer;

	[Export]  // These are for smooth movement
	private double moveRepeatDelay = 0.25;
	[Export] 
	private double moveRepeatInterval = 0.08;

	private double moveRepeatTimer;
	private int heldMoveDirection;
	


 // For better debugging
	public override void _Draw()
	{
		foreach (Vector2I coord in currentBlock.gridCoords())
		{
			var rect = new Rect2(
				coord.X * cellSize,
				coord.Y * cellSize,
				cellSize,
				cellSize
			);

			DrawRect(rect, new Color(0, 0, 1, 0.25f), true);
			DrawRect(rect, Colors.Blue, false, 1.0f);
		}
	}





	private int thresholdRow = 2;
	private readonly RandomNumberGenerator gen = new();
 


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		curCooldown = delay;
		currentBlock = GetNextBlock(4);
		currentBlock.Visible = true;
		nextBlock = GetNextBlock(1);
	
	}
	

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (wiping) return;
		if(curCooldown <= 0){
			FallBlock();	
		}else{
			curCooldown -= delta;
		}
		HandleHorizontalInput(delta);
		QueueRedraw(); //for debugging block
	}
	
	public override void _Input(InputEvent @event)
	{
		if (wiping) return;
		if (@event.IsActionPressed("rotate"))
			RotateBlock();
		else if (@event.IsActionPressed("move_down"))
			FallBlock();
		else if (@event.IsActionPressed("hard_drop"))
		HardDrop();
	}


	private void HardDrop()
	{
		Block dropping = currentBlock;
		while (currentBlock == dropping)
			FallBlock(); //This was pure torture for me 4 years ago. 
			
	}
	private void HandleHorizontalInput(double delta)
	{
		bool left = Input.IsActionPressed("move_left");
		bool right = Input.IsActionPressed("move_right");

		int direction = left == right ? 0 : (left ? -1 : 1);

		if (direction == 0)
		{
			heldMoveDirection = 0;
			moveRepeatTimer = 0;
			return;
		}

		if (direction != heldMoveDirection)
		{
			heldMoveDirection = direction;
			MoveBlock(direction < 0);
			moveRepeatTimer = moveRepeatDelay;
			return;
		}

		moveRepeatTimer -= delta;
		if (moveRepeatTimer <= 0)
		{
			MoveBlock(direction < 0);
			moveRepeatTimer = moveRepeatInterval;
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
		placeSound?.Play();
		currentBlock.GetParent().AddToGroup("placed");
		
		
		foreach(Vector2I coord in currentBlock.gridCoords()){
			if(coord.Y <= thresholdRow){
				StartWipe();
				//ResetGrid();
				break;
			}
			grid[coord.Y, coord.X] = 1;	
		}
		currentBlock = nextBlock;
		currentBlock.Visible = true;
		nextBlock = GetNextBlock();
	}
	
	private void ResetGrid(){
		GetTree().CallGroup("placed", "queue_free");

		int score = 0;
		for(int y = 0; y < grid.GetLength(0); ++y)
		{
			for(int x = 0; x < grid.GetLength(1); ++x)
			{
				if(grid[y, x] != 0) score += 50;
				grid[y, x] = 0;
			}
		}
		scorer.UpdateScore(score);
	}
	
	private Block GetNextBlock(int index=0){
		//index = gen.RandiRange(0, blocks.Length - 1);
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
	
	private void RotateBlock()
	{
		var originalCoords = currentBlock.coords.Duplicate();
		var originalPivot = currentBlock.pivot;
		Vector2 delta = currentBlock.rotateInternal();

		foreach (Vector2I coord in currentBlock.gridCoords())
		{
			if (coord.X < 0 || coord.Y < 0 ||
				coord.X >= grid.GetLength(1) ||
				coord.Y >= grid.GetLength(0) ||
				grid[coord.Y, coord.X] == 1)
			{
				currentBlock.coords = originalCoords;
				currentBlock.pivot = originalPivot;
				return;
			}
		}

		Node2D parent = currentBlock.GetParent<Node2D>();
		parent.Rotate(Mathf.Pi / 2);
		parent.Position += delta * cellSize;  
	}

	

	
	private void MoveBlock(bool left){
		int dir = left ? -1 : 1;
		foreach(Vector2I coord in currentBlock.gridCoords()){
			int newX = coord.X + dir;

			if(newX < 0 || newX >= grid.GetLength(1) || grid[coord.Y, newX] == 1){
				return;
			}
		}
		Node2D parent = currentBlock.GetParent<Node2D>();
		parent.Position += new Vector2(cellSize * dir, 0);
		currentBlock.offsetInternal += new Vector2I(dir, 0);
	}

	private void StartWipe(){
		wiping = true;
		heldMoveDirection = 0;
		moveRepeatTimer = 0; //Pauses

		Vector2 boardSize = new Vector2(grid.GetLength(1), grid.GetLength(0)) * cellSize;
		wipeSprite.Centered = false;
		wipeSprite.Scale = boardSize / wipeSprite.Texture.GetSize();
		wipeSprite.ZIndex = 100;
		wipeSprite.Position = new Vector2(0, -boardSize.Y);   //Intial opiosn
		wipeSprite.Visible = true;

		var tween = CreateTween();
		tween.TweenProperty(wipeSprite, "position", Vector2.Zero, wipeDuration);                      // slide in
		tween.TweenCallback(Callable.From(ResetGrid));                                                // clear while covered
		tween.TweenProperty(wipeSprite, "position", new Vector2(0, -boardSize.Y), wipeDuration);       // slide out
		tween.TweenCallback(Callable.From(() => {
			wipeSprite.Visible = false;
			wiping = false;
			curCooldown = delay;
		}));
	}
}