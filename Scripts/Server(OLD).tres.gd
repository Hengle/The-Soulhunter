extends Node

var players = {}
var rooms = {}

var sync_timeout = 0

func _ready():
	Com.server = self

func _process(delta):
	sync_timeout += delta
	if sync_timeout >= 2:
		for room in rooms.values():
#			print("XXX")
			for player in room.players.get_children():
#				print(" >>>> ", player)
				synchronize_player(player.mapid, player.id, true)
		sync_timeout = 0

func create_room(id):
	print("New room: " + str(id))
	var game = load("res://Scenes/InGame.tscn").instance()
	rooms[id] = {"root": game, "players": game.get_node("Players")}
	
	var viewport = Viewport.new()
	viewport.world = World2D.new()
	add_child(viewport)
	viewport.add_child(game)
	game.load_map(id)

func remove_room(id):
	print("Removing room: " + str(id))
	
	rooms[id].root.get_viewport().queue_free()
	rooms.erase(id)

func add_player(id, room):
	players[id] = {"id": id, "room": room}

func synchronize_player(mapid, id, forced = false):
	for player in Com.server.rooms[mapid].players.get_children():
		if forced or player.id != id:
			Network.send_data(["PRIVSYNC", mapid, id, "p", player.id, player.position])
	
	for enemy in Com.server.rooms[mapid].root.enemies.get_children():
		var ary = enemy.get_sync_data()
		if ary.empty(): continue
		Network.send_data(["PRIVSYNC", mapid, id, "e", enemy.id] + ary)

func sync_to_all(type, object):
	if type == "e":
		var ary = object.get_sync_data()
		if ary.empty(): return
		Network.send_data(["BROADSYNC", object.mapid, "e", object.id] + ary)