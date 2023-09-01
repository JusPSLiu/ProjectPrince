extends Particles2D


onready var timeCreated = Time.get_ticks_msec()

func _process(_delta):
	if Time.get_ticks_msec() - timeCreated > 1000:
		for n in get_children():
			remove_child(n)
			n.queue_free()
		queue_free()
