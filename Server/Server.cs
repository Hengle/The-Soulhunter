using Godot;
using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class Server : Node {
    private static readonly PackedScene roomFactory = ResourceLoader.Load("res://Server/Nodes/Room.tscn") as PackedScene;
    private List<PackedScene> mapList;

    private TcpListener server;
    private Database database;
    private static Server instance;

    private Dictionary<int, List<Room>> rooms;
    private List<Player> playersOnline;
    private Node controls;

    private bool available = true;

    public override void _Ready() {
        instance = this;
        server = new TcpListener(IPAddress.Parse("127.0.0.1"), 2412);
        database = new Database();

        rooms = new Dictionary<int, List<Room>>();
        playersOnline = new List<Player>();

        mapList = new List<PackedScene>();
        foreach (var map in System.IO.Directory.GetFiles("Maps"))
            mapList.Add(ResourceLoader.Load(map) as PackedScene);
        
        controls = GetNode("/root/Com/Controls");
        GetNode("/root/Com").Set("is_server", true);

        server.Start();
    }

    public override void _Process(float delta) {
        if (available) {
            available = false;

            server.AcceptTcpClientAsync().ContinueWith((client) => {
                var thread = new System.Threading.Thread(ClientLoop);
                thread.Start(client.Result);
                available = true;
            });
        }
    }

    private void ClientLoop(object _client) {
        var client = _client as TcpClient;

        NetworkStream stream = client.GetStream();
        Player player = new Player(stream);
        new Packet(Packet.TYPE.HELLO).Send(stream);

        var bytes = new byte[256];

        GD.Print("Player connected");

        while (true) {
            try {
                int read = stream.Read(bytes, 0, bytes.Length);

                if (read == 0) {
                    GD.Print("Connection closed");
                    player.LogOut();
                    client.Close();
                    return;
                }
            } catch (System.IO.IOException) {
                GD.Print("Connection error");
                player.LogOut(); //tutaj np. dodanie do hanged connections i czekanie sobie, zamiast logout
                client.Close();
                return;
            }

            var unpacker = new Unpacker(bytes);

            GD.Print("Received packet: " + ((Packet.TYPE)unpacker.GetCommand()).ToString());

            unpacker.HandlePacket(database, player);
        }
        
        // client.Close();
    }

    public static Room GetAdjacentMap(int x, int y) {
        foreach (var map in instance.mapList) {
            var m_x = (int)map.GetState().GetNodePropertyValue(0, 3); //niebezpieczny szajs; może dać resource?
            var m_y = (int)map.GetState().GetNodePropertyValue(0, 4);
            var m_w = (int)map.GetState().GetNodePropertyValue(0, 5);
            var m_h = (int)map.GetState().GetNodePropertyValue(0, 6);

            if (x >= m_x && y >= m_y && x < m_x + m_w && y < m_y + m_h) {
                return instance.GetRoom((ushort)(int)GD.Convert(map.GetState().GetNodePropertyValue(0, 2), (int)Variant.Type.Int));
            }
        }

        return null;
    }

    public Room GetRoom(ushort mapId) {
        if (!rooms.ContainsKey(mapId) || rooms[mapId].Count == 0) {
            return CreateRoom(mapId);
        }

        return rooms[mapId][0]; //tutaj wybór fajnego pokoju
    }

    public static void RemoveRoom(ushort mapId, Room room) {
        instance.rooms[mapId].Remove(room);
        room.QueueFree();
    }

    private Room CreateRoom(ushort mapId) {
        if (!rooms.ContainsKey(mapId)) {
            rooms.Add(mapId, new List<Room>());
        }

        var room = roomFactory.Instance() as Room;
        room.SetMap(mapId);
        AddChild(room);
        rooms[mapId].Add(room);

        return room;
    }

    public static void RemoveOnlinePlayer(Player player) {
        instance.playersOnline.Remove(player);
    }

    public static void AddOnlinePlayer(Player player) {
        instance.playersOnline.Add(player);
    }

    public static Player GetPlayerOnline(string login) { //tu lepiej jakiś słownik
        return instance.playersOnline.Find((player) => player.GetLogin() == login);
    }

    public static List<Player> GetPlayers() {
        return instance.playersOnline;
    }

    public static Server Instance() {return instance;}
    public static Node GetControls() {return instance.controls;}    
}