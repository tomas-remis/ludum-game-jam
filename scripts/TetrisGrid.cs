using Godot;
using System;

public partial class TetrisGrid : Node2D
{
	[Export]
	private Label clockLabel;
	private int totalScore;

	[Export]
	private double lastMinuteDelay = 0.25;  
	private bool lastMinute;

	[Export(PropertyHint.File, "*.tscn")]
	private string menuScene = "res://scripts/main_menu.tscn";

	private double clockSeconds;  
	private bool timeUp;

	[Export]
	private Sprite2D wipeSprite;

	[Export]
	private Texture2D gameOverWipeTex;

	[Export]
	private double wipeDuration = 2.5;
	private bool wiping;



	public Block currentBlock;

	private Block _nextBlock;
	private Block nextBlock
	{
		get => _nextBlock;
		set
		{
			_nextBlock = value;
			nextPreview.UpdateSprite(nextBlock.Texture);
		}
	}
	private int[,] grid = new int[17, 15];

	[Export]
	private AudioStreamPlayer placeSound;

	[Export]
	private AudioStreamPlayer musicPlayer;

	[Export]
	private Godot.Collections.Array<AudioStreamWav> music;

	[Export]
	private HookPosition hookController;


	private double curCooldown;
	private bool musicSwapped = false;

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

	[Export]
	private PiecePreviewer nextPreview;

	private double moveRepeatTimer;
	private int heldMoveDirection;

	private Action handler;



	// For better debugging
	public override void _Draw()
	{
		if (currentBlock is not null)
		{
			foreach (Vector2I coord in currentBlock.gridCoords())
			{
				var rect = new Rect2(
					coord.X * cellSize,
					(coord.Y - 0.5f) * cellSize,
					cellSize,
					cellSize
				);

				DrawRect(rect, new Color(0, 0, 0, 0.4f), true);
				DrawRect(rect, new Color(1, 1, 1, 0.2f), false, 3.0f); //outline????? 
			}
		}
	}


	private int thresholdRow = 2;
	private readonly RandomNumberGenerator gen = new();



	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		// place wiper at initial position
		Vector2 boardSize = new Vector2(grid.GetLength(1), grid.GetLength(0)) * cellSize;
		wipeSprite.Centered = false;
		wipeSprite.Scale = (boardSize + new Vector2(2, 6) * cellSize) / wipeSprite.Texture.GetSize();
		wipeSprite.ZIndex = 100;
		wipeSprite.Position = new Vector2(-cellSize, -boardSize.Y - 4 * cellSize);   //Intial position

		curCooldown = lastMinute ? lastMinuteDelay : delay;
		currentBlock = GetNextBlock();
		currentBlock.Visible = true;

		if (!nextPreview.IsNodeReady())
		{
			await ToSignal(nextPreview, Node.SignalName.Ready);
		}

		nextBlock = GetNextBlock();

		musicPlayer.Stream = music[0];
		musicPlayer.Play();

		handler = () => musicPlayer.Play();
		musicPlayer.Finished += handler;
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdateClock(delta);
		if (wiping) return;
		if (curCooldown <= 0)
		{
			FallBlock();
		}
		else
		{
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
		{
			GD.Print(currentBlock.offsetInternal);
			FallBlock(); //This was pure torture for me 4 years ago.
		}


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

	private void FallBlock()
	{
		curCooldown = lastMinute ? lastMinuteDelay : delay;
		foreach (Vector2I coord in currentBlock.gridCoords())
		{
			int newY = coord.Y + 1;

			if (newY >= grid.GetLength(0) || grid[newY, coord.X] == 1)
			{
				PlaceBlock();
				return;
			}
		}
		Node2D parent = currentBlock.GetParent<Node2D>();

		parent.Position += new Vector2(0, cellSize);
		hookController.UpdateHookPosition(parent);
		currentBlock.offsetInternal += new Vector2I(0, 1);
	}

	private void PlaceBlock()
	{
		placeSound?.Play();
		currentBlock.GetParent().AddToGroup("placed");


		foreach (Vector2I coord in currentBlock.gridCoords())
		{
			if (coord.Y <= thresholdRow)
			{
				StartWipe();
				//ResetGrid();
				return;
			}
			grid[coord.Y, coord.X] = 1;
		}
		currentBlock = nextBlock;

		hookController.UpdateHookPosition(currentBlock.GetParent<Node2D>());
		currentBlock.Visible = true;
		nextBlock = GetNextBlock();
	}

	private void ResetGrid()
	{
		GetTree().CallGroup("placed", "queue_free");


		currentBlock = nextBlock;
		hookController.UpdateHookPosition(currentBlock.GetParent<Node2D>());
		currentBlock.Visible = true;
		nextBlock = GetNextBlock();

		int score = 0;
		for (int y = 0; y < grid.GetLength(0); ++y)
		{
			for (int x = 0; x < grid.GetLength(1); ++x)
			{
				if (grid[y, x] != 0) score += 50;
				grid[y, x] = 0;
			}
		}
		scorer.UpdateScore(score);
		totalScore += score;
	}

	private Block GetNextBlock()
	{
		int index = gen.RandiRange(0, blocks.Length - 1);
		//int index = 1;
		Node2D parent = blocks[index].Instantiate() as Node2D;
		Block block = parent.FindChild("Sprite", recursive: true, owned: false) as Block;
		block.Visible = false;

		int offsetInternalY = 0;
		int offsetInternalX = grid.GetLength(1) / 2;
		foreach (Vector2I coord in block.coords)
		{
			offsetInternalY = Math.Min(offsetInternalY, coord.Y);
		}
		block.offsetInternal = new Vector2I(offsetInternalX, offsetInternalY);
		block.AdjustScale(cellSize);

		parent.Position = new Vector2(
			offsetInternalX + block.pivot.X,
			offsetInternalY + block.pivot.Y - 0.5f
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
		hookController.UpdateHookPosition(parent);
	}




	private void MoveBlock(bool left)
	{
		int dir = left ? -1 : 1;
		foreach (Vector2I coord in currentBlock.gridCoords())
		{
			int newX = coord.X + dir;

			if (newX < 0 || newX >= grid.GetLength(1) || grid[coord.Y, newX] == 1)
			{
				return;
			}
		}
		Node2D parent = currentBlock.GetParent<Node2D>();
		parent.Position += new Vector2(cellSize * dir, 0);
		hookController.UpdateHookPosition(parent);
		currentBlock.offsetInternal += new Vector2I(dir, 0);
	}

	private void StartWipe(bool stayDown = false)
	{
		currentBlock = null;
		hookController.UpdateHookPosition(this);

		wiping = true;
		heldMoveDirection = 0;
		moveRepeatTimer = 0; //Pauses

		Vector2 boardSize = new Vector2(grid.GetLength(1), grid.GetLength(0)) * cellSize;
		wipeSprite.Centered = false;
		wipeSprite.Scale = (boardSize + new Vector2(2, 6) * cellSize) / wipeSprite.Texture.GetSize();
		wipeSprite.ZIndex = 100;
		wipeSprite.Position = new Vector2(-cellSize, -boardSize.Y - 4 * cellSize);   //Intial position

		// wipeSprite.Visible = true;

		var tween = CreateTween();

		// slide in
		tween.TweenProperty(wipeSprite, "position", new Vector2(-cellSize, -6 * cellSize), wipeDuration)
			.SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.InOut);
		tween.TweenCallback(Callable.From(ResetGrid)); // clear while covered

		if (stayDown)
		{
			return;
		}

		// slide out
		tween.TweenProperty(wipeSprite, "position", new Vector2(-cellSize, -boardSize.Y - 4 * cellSize), wipeDuration)
			.SetDelay(0.5f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.InOut);
		tween.TweenCallback(Callable.From(() =>
		{
			// wipeSprite.Visible = false;
			wiping = false;
			curCooldown = lastMinute ? lastMinuteDelay : delay;
			
		}));
	}


	private void UpdateClock(double delta){
	if (timeUp) return;

	clockSeconds += delta;

	double gameMinutes = 10* 60 + clockSeconds * 3;
	lastMinute = gameMinutes >= 15 * 60;

	if(gameMinutes >= 17 * 60 - music[1].GetLength() * 3 && !musicSwapped){
		musicSwapped = true;
		musicPlayer.Stream = music[1];
		musicPlayer.Play();
		musicPlayer.Finished -= handler;
	}

	if (gameMinutes >= 17 * 60){
		scorer.UpdateScore(10);  // final update
		timeUp = true;
		GetTree().Root.SetMeta("final_score", totalScore);
		wipeSprite.Texture = gameOverWipeTex;
		StartWipe(true);

		SceneTreeTimer timer = GetTree().CreateTimer(5f);
		timer.Timeout += () => GetTree().ChangeSceneToFile(menuScene);
		return;
	}

	int hour = (int)(gameMinutes / 60);
	int minute = (int)(gameMinutes % 60);
	int hour12 = hour % 12 == 0 ? 12 : hour % 12;
	string suffix = hour < 12 ? "AM" : "PM";

	clockLabel.Text = $"{hour12}:{minute:00} {suffix}";
	}
}
