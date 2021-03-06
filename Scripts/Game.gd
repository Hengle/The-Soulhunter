extends Node2D

var map
var last_room

var last_enemy = -1
var is_menu = false

onready var entities = $Entities
onready var effects = $Effects

var entity_list = {}
var special_entity_list = {}

signal player_joined

func _ready():
	if !Com.is_server:
		Network.connect("game_over", self, "on_over")
		Com.controls.state = Controls.State.ACTION
		get_tree().set_auto_accept_quit(false)

func load_map(id):
	map = Res.maps[id].instance()
	add_child(map)

func change_map(id):
	if map:
		map.queue_free()
		for entity in entities.get_children():
			entity.queue_free()
	
	load_map(id)

func add_main_player(player):
	player.connect("initiated", self, "start")
	add_child(player)
	player.connect("reg_mp", player.get_node("PlayerCamera/UI"), "reg_mp")

func add_entity(type, id):
	var node = load(str("res://Nodes/", Data.NODES[type], ".tscn")).instance()
	node.set_meta("valid", true)
	node.set_meta("id", id)
	
	entity_list[id] = node
	entities.add_child(node)
	if node.has_method("on_client_create"):
		node.on_client_create()
	else:
		node.visible = false
		node.set_process(false)
		node.set_physics_process(false)

func register_special_entity(entity):
	special_entity_list[entity.get_meta("id")] = entity

func register_entity(node, id):
	entity_list[id] = node

func remove_entity(id):
	var entity = entity_list.get(id)
	if entity:
		entity_list.erase(id)
		entity.queue_free()

func get_entity(id):
	return entity_list.get(id)

func get_special_entity(id):
	return special_entity_list.get(id)

func on_over(time):
	var game_over = preload("res://Scenes/GameOver.tscn").instance()
	game_over.get_node("Title").visible = false
	game_over.set_time(time)
	Com.player.visible = false
	Com.player.set_process(false)
	Com.player.set_physics_process(false)
	add_child(game_over)

func start(): ##:/
	Com.player.get_node("PlayerCamera/Fade/ColorRect").color.a = 0