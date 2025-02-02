extends Particles2D


onready var timeCreated = Time.get_ticks_msec()
export var deciseconds : int

func _process(_delta):
	if Time.get_ticks_msec() - timeCreated > deciseconds*100:
		for n in get_children():
			remove_child(n)
			n.queue_free()
		queue_free()
