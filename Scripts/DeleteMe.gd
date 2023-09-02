extends Node


onready var timeCreated = Time.get_ticks_msec()
export var deciseconds : int

func _process(_delta):
	if Time.get_ticks_msec() - timeCreated > deciseconds*100:
		queue_free()
