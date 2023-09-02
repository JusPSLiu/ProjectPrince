extends AudioStreamPlayer


# Declare member variables here. Examples:
# var a = 2
# var b = "text"
export var chasemusic : AudioStreamMP3

# Called when the node enters the scene tree for the first time.
func _ready():
	pass



func _on_TextureButton_pressed():
	stop()
	self.stream = load("res://Sounds/Music/Level2ChaseSequence.mp3")
	play()
