using Godot;
using System;

public class PlayerAttack : RigidBody2D
{
	// magicSpeed - Speed at which a magic bolt moves
	[Export] int magicSpeed;

	private Direction attackDirection;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// change sprite orientation based on player direction
		if(attackDirection == Direction.Left)
        {
			GetNode<AnimationTree>("AnimationTree").Set("parameters/Moving/blend_position", -1);
			magicSpeed *= -1;
		}
	}

	//Can be thought as being run every frame. Delta is the amount of time it took each frame to be made (This should be constant)
	public override void _PhysicsProcess(float delta)
	{
		//Every frame, moves the projectile according to its active speed
		GlobalPosition = new Vector2(GlobalPosition.x + magicSpeed, GlobalPosition.y);
	}

	//If the PlayerAttack touches any walls or enemies, this method is called
	//Node body is the object this collided with
	public void _on_PlayerAttack_body_entered(Node body)
	{
		//Checks if the touched object was an enemy, if so, deletes it
		if (body.IsInGroup("Enemy"))
		{
			((BaseEnemy)body).Killed();
		}
		else if (body.IsInGroup("Rival"))
		{
			(body as Rival).GotHit();
		}

		//delet all the kids
		foreach (Node n in GetChildren()) {
			RemoveChild(n);
			n.QueueFree();
		}

		//vertical explosion
		Particles2D particles = (Particles2D)ResourceLoader.Load<PackedScene>("res://Particles//Vertplosion.tscn").Instance();
		particles.Position = GlobalPosition;
		particles.Emitting = true;
		GetTree().CurrentScene.AddChild(particles);

		//Deletes itself
		QueueFree();
	}

	public void SetDirection(Direction attackDirection)
    {
		this.attackDirection = attackDirection;
    }
}
