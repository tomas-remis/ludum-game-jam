extends Control

var main_menu : VBoxContainer
var credits : Panel

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	main_menu = get_child(1)
	credits = get_child(2)


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass


func start_pressed() -> void:
	get_tree().change_scene_to_file("res://main.tscn")

func toggle_credits() -> void:
	main_menu.visible = not main_menu.visible
	credits.visible = not credits.visible
	print(credits.visible)
	
	

func exit_button() -> void:
	get_tree().quit() # Replace with function body.
