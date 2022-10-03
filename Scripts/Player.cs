using Godot;
using System;

public class Player : KinematicBody2D
{
	/*
	 * Exports
	 * 
	 * xAcceleration - Changes how fast the player accelerates left/right
	 * maxHSpeed - Determines the maximum speed the player can go left/right
	 * maxVSpeed - Determines the minimum speed the player can go up/down
	 * jumpPower - Determines how high the player goes when they jump
	 * gravity - pulls the player down at a constant rate
	 * wallJumpStrength - 
	 * 
	 * Animation Fields
	 * 
	 */

	[Export] public int xAcceleration;
	[Export] public int maxHSpeed;
	[Export] public int maxVSpeed;
	[Export] public int jumpPower;
	[Export] public int gravity;
	[Export] public int wallJumpStrength;
	[Export] public int wallFallingSpeed;
	[Export] public int numExtraJumpFrames;
	[Export] public int pushStrength;
	[Export] public int climbSpeed;

	//Animation Fields
	private AnimationTree playerAnimationTree;
	private AnimationNodeStateMachinePlayback playerANSMP;

	//Movement Fields
	private Vector2 velocity = new Vector2();
	private Vector2 floor = new Vector2(0, -1);
	private Vector2 lastDirection = new Vector2(0, 0);
	private bool movingHorizontally;
	private int framesSinceMissingFloor;
	private int numRightWalls;
	private int numLeftWalls;
	private bool onFloor;
	private bool onMoveableObject;
	private bool movingAnObject;
	private int numLadders;

	//Attack Fields
	public PackedScene PlayerProjectilePath;
	private bool swordEnabled;
	private bool rangedAttackEnabled;
	private bool magicJumpEnabled;

	// Debug Menu References
	Control DebugMenu;
	Tabs PlayerMenu;
	Label Pos, XVelocity, YVelocity, IsAttacking, IsWallJumping, IsWallClimbing, OnWall, OnFloor, OnCeiling, SwordHitboxL, Swinging;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		playerAnimationTree = GetNode<AnimationTree>("AnimationTree");
		playerANSMP = playerAnimationTree.Get("parameters/playback") as AnimationNodeStateMachinePlayback;

		framesSinceMissingFloor = numExtraJumpFrames + 1;

		PlayerProjectilePath = GD.Load<PackedScene>("res://Scenes/PlayerAttack.tscn");

		// Debug menu vars
		DebugMenu = GetNode<Control>("DebugMenu/DebugMenu");
		PlayerMenu = DebugMenu.GetNode<Tabs>("TabContainer/Player");
		Pos = PlayerMenu.GetNode<Label>("Left/Position");
		XVelocity = PlayerMenu.GetNode<Label>("Left/XVelocity");
		YVelocity = PlayerMenu.GetNode<Label>("Left/YVelocity");
		IsAttacking = PlayerMenu.GetNode<Label>("Left/IsAttacking");
		IsWallJumping = PlayerMenu.GetNode<Label>("Left/IsWallJumping");
		IsWallClimbing = PlayerMenu.GetNode<Label>("Left/IsWallClimbing");
		OnWall = PlayerMenu.GetNode<Label>("Right/IsOnWall");
		OnFloor = PlayerMenu.GetNode<Label>("Right/IsOnFloor");
		OnCeiling = PlayerMenu.GetNode<Label>("Right/IsOnCeiling");
		SwordHitboxL = PlayerMenu.GetNode<Label>("Right/SwordHitboxL");
		Swinging = PlayerMenu.GetNode<Label>("Right/Swinging");
	}

	//Can be thought as being run every frame. Delta is the amount of time it took each frame to be made (This should be constant)
	public override void _PhysicsProcess(float delta) {
		//Changes the Character's movement velocity
		//MoveCharacterOld(delta);
		MoveCharacter(delta);

		//Shoots a projectile
		//AttackOld(delta);
		Attack(delta);
		//Changes the Character's animations
		AnimatePlayer();
	}

	public void MoveCharacter(float delta)
	{
		movingAnObject = false;
		movingHorizontally = false;
		framesSinceMissingFloor++;

		//Interaction with moveable objects
		//Gets the number of "Slides" and checks each one
		for (int i = 0; i < GetSlideCount(); i++)
		{
			//Sets the collision as a variable
			KinematicCollision2D collision = GetSlideCollision(i);
			//After Reset, the Collider is sometimes null, so check for it
			if (collision.Collider == null)
			{
				//If the collision is empty, skips to next collision
				continue;
			}
			//Checks if the collision was with a moveableObject
			if ((collision.Collider as Node).IsInGroup("MoveableObject"))
			{
				//Sets the moving object as a variable
				RigidBody2D moveableObject = collision.Collider as RigidBody2D;
				//Sets a directional Impulse
				moveableObject.ApplyCentralImpulse(new Vector2(-collision.Normal.x * pushStrength, 0));
				movingAnObject = true;
			}
		}

		if (IsOnWall() && !movingAnObject)
		{
			velocity.x = 0;
		}

		//Input for Left/Right movement
		if (Input.IsActionPressed("move_left") == Input.IsActionPressed("move_right"))
		{
			// If currently still going right, apply left force until it cancels out
			if (velocity.x > 0)
			{
				velocity.x -= xAcceleration;
				if (velocity.x <= 0)
				{
					velocity.x = 0;
					lastDirection.x = 1;
				}
			}
			// If currently still going left, apply right force until it cancels out
			else if (velocity.x < 0)
			{
				velocity.x += xAcceleration;
				if (velocity.x >= 0)
				{
					velocity.x = 0;
					lastDirection.x = -1;
				}
			}
		}
		else if (Input.IsActionPressed("move_left"))
		{
			velocity.x = Mathf.Clamp(velocity.x -= xAcceleration, -maxHSpeed, maxHSpeed);
			movingHorizontally = true;
			lastDirection.x = -1;
		}
		else if (Input.IsActionPressed("move_right"))
		{
			velocity.x = Mathf.Clamp(velocity.x += xAcceleration, -maxHSpeed, maxHSpeed);
			movingHorizontally = true;
			lastDirection.x = 1;
		}

		//Input for Up/Down Movement
		if (IsOnCeiling())
		{
			velocity.y = 0;
		}

		if (!IsOnFloor() && !onMoveableObject)
        {
			velocity.y = Mathf.Clamp(velocity.y += gravity, -maxVSpeed, maxVSpeed);

		}
        else
        {
			framesSinceMissingFloor = 0;
			velocity.y = 0;
		}
		if (Input.IsActionPressed("move_jump") && (onFloor || onMoveableObject || framesSinceMissingFloor <= numExtraJumpFrames))
        {
			framesSinceMissingFloor = numExtraJumpFrames + 1;
			velocity.y = Mathf.Clamp(velocity.y = -jumpPower, -maxVSpeed, maxVSpeed);
		}
		else if (!onFloor && !onMoveableObject && (numRightWalls >= 2 || numLeftWalls >= 2))
		{
			if (Input.IsActionJustPressed("move_jump") && ((Input.IsActionPressed("move_right") && numRightWalls >= 2) || (Input.IsActionPressed("move_left") && numLeftWalls >= 2)))
			{
				lastDirection.x = -lastDirection.x;
				velocity.x += lastDirection.x * (xAcceleration * wallJumpStrength);
				velocity.y = -jumpPower;
			}
			else if (numLadders > 0 && (Input.IsActionPressed("move_left") || Input.IsActionPressed("move_right")))
			{
				velocity.y = -climbSpeed;
			}
			else
            {
				velocity.y = Mathf.Clamp(velocity.y, -maxVSpeed, wallFallingSpeed);
			}
		}
		else if(numLadders > 0 && (Input.IsActionPressed("move_left") || Input.IsActionPressed("move_right")))
        {
			velocity.y = -climbSpeed;
        }
		//"false, 4, 0.78598f" are default values
		//Last argument is for infinite_inertia, We turn this off so environment can be interacted with
		MoveAndSlide(velocity, floor, false, 4, 1.178097f, false);

		updateDMenu();
    }

	public void Attack(float delta)
    {
		//Ranged Attack
        if (rangedAttackEnabled && Input.IsActionJustPressed("attack"))
        {
			PlayerAttack RangedAttackInstance = PlayerProjectilePath.Instance() as PlayerAttack;
			if(lastDirection.x >= 0)
            {
				RangedAttackInstance.GlobalPosition = GetNode<Position2D>("RProjectilePosition").GlobalPosition;
				RangedAttackInstance.SetDirection(Direction.Right);
            }
            else
            {
				RangedAttackInstance.GlobalPosition = GetNode<Position2D>("LProjectilePosition").GlobalPosition;
				RangedAttackInstance.SetDirection(Direction.Left);
			}
            GetParent().AddChild(RangedAttackInstance);
		}
    }

	public void AnimatePlayer()
    {
		if(velocity.x == 0 && !movingHorizontally)
        {
			playerANSMP.Travel("Idle");
        }
        else
        {
			playerANSMP.Travel("Run");
		}
		playerAnimationTree.Set("parameters/" + playerANSMP.GetCurrentNode() + "/blend_position", lastDirection.x);
    }

	public void updateDMenu()
	{
		Sprite playerSprite = GetNode<Sprite>("Sprite");
		Pos.Text = "POS: " + "(X: " + playerSprite.GlobalPosition.x.ToString() + ", Y: " + playerSprite.GlobalPosition.y.ToString() + ")";
		XVelocity.Text = "XVel: " + velocity.x.ToString();
		YVelocity.Text = "YVel: " + velocity.y.ToString();
		//IsAttacking.Text = "Attacking: " + (shotTimePassed > 0).ToString();
		//IsWallJumping.Text = "WallJumping: " + wallJumping.ToString();
		//IsWallClimbing.Text = "WallClimbing: " + wallClimbing.ToString();
		OnWall.Text = "OnWall: " + IsOnWall().ToString();
		OnFloor.Text = "OnFloor: " + onFloor.ToString();
		OnCeiling.Text = "OnCeiling: " + IsOnCeiling().ToString();
		//SwordHitboxL.Text = "SH-POS: " + "(X: " + SwordHitbox.GlobalPosition.x.ToString() + ", Y: " + SwordHitbox.GlobalPosition.y.ToString() + ")";

		if (Input.IsActionJustPressed("ui_debug"))
		{
			DebugMenu.Visible = !DebugMenu.Visible;
		}
	}

	public void _on_SwordHitbox_body_entered(Node body) {
		//Removed
	}

	public void MoveCamera(Vector2 newGlobalPosition)
	{
		GetNode<Camera2D>("Camera2D").GlobalPosition = newGlobalPosition;
	}

	public void OnWalljumpBodyTouched(Node body, bool entered, bool right)
    {
        if (body.Name.Equals("Baseground"))
        {
            if (right)
            {
				numRightWalls += entered ? 1 : -1;
			}
            else
            {
				numLeftWalls += entered ? 1 : -1;
			}
		}
		else if (body.IsInGroup("WallClimbable"))
        {
			if (right)
			{
				numRightWalls += entered ? 1 : -1;
			}
			else
			{
				numLeftWalls += entered ? 1 : -1;
			}
			numLadders += entered ? 1 : -1;
		}
    }

	public void OnFloorDetectorBodyTouched(Node body, bool entered)
    {
		if (body.Name.Equals("Baseground"))
		{
			onFloor = entered ? true : false;
		}
		else if (body.IsInGroup("MoveableObject"))
        {
			onMoveableObject = entered ? true : false;
		}
	}

	public void SetPlayerAbility(bool add, bool clearFirst, int abilityID)
    {
        if (clearFirst)
        {
			swordEnabled = false;
			rangedAttackEnabled = false;
			magicJumpEnabled = false;
		}
        switch (abilityID)
        {
			case 0:
				swordEnabled = add ? true : false;
				break;
			case 1:
				rangedAttackEnabled = add ? true : false;
				break;
			case 2:
				magicJumpEnabled = add ? true : false;
				break;
			case 3:
				swordEnabled = false;
				rangedAttackEnabled = false;
				magicJumpEnabled = false;
				break;
			case 4:
				swordEnabled = true;
				rangedAttackEnabled = true;
				magicJumpEnabled = true;
				break;
			default:
				break;
		}
    }
}



