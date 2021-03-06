using Godot;
using System;
using System.Net.Sockets;
using MongoDB.Driver;
using MongoDB.Bson;

public class Database {
    private MongoClient client;
    private IMongoDatabase database;

    public Database() {
        client = new MongoClient("mongodb://localhost");
        database = client.GetDatabase("the_soulhunter");
    }

    public Error RegisterUser(string login, string password, ushort hue) {
        var collection = database.GetCollection<BsonDocument>("players");

        if (collection.CountDocuments(new BsonDocument {{"login", login}} ) == 1) {
            return Error.FileAlreadyInUse;
        }

        collection.InsertOne(new BsonDocument {
            {"login", login},
            {"password", password},
            {"hue", hue} //po prostu nie ;_;
        } );

        return Error.Ok;
    }

    public Error TryLogin(string login, string password, Player player) {
        var collection = database.GetCollection<BsonDocument>("players");
        var found = collection.Find(new BsonDocument {{"login", login}} ).FirstOrDefault();

        if (found == null) {
            return Error.FileNotFound;
        }

        if (Server.GetPlayerOnline(login) != null) {
            return Error.Busy;
        }

        if (found.GetValue("password") != password) {
            return Error.FileNoPermission;
        }

        player.LogIn(found);
        player.SetCharacter(login);
        Server.AddOnlinePlayer(player);

        return Error.Ok;
    }
    
    private BsonDocument getDefaultData(string name = "", BsonDocument player = null) {
        var data = new BsonDocument {
            {"name", name},
            {"location", 6},
            {"level", 1},
            {"exp", 0},
            {"hp", 120},
            {"max_hp", 120},
            {"mp", 80},
            {"max_mp", 80},
            {"attack", 1},
            {"defense", 1},
            {"magic_attack", 1},
            {"magic_defense", 1},
            {"luck", 1},
            {"inventory", new BsonArray()},
            {"equipment", new BsonArray {0, 0, 0, 0, 0, 0, 0, 0}},
            {"souls", new BsonArray {0}},
            {"soul_equipment", new BsonArray {0, 0, 0, 0, 0, 0, 0}},
            {"abilities", 0},
            {"chests", new BsonArray()},
            {"discovered", new BsonArray()},
            {"game_over", 0}
        };

        if (player != null) data.SetElement(new BsonElement("hue", player.GetValue("hue").AsInt32));
        else data.SetElement(new BsonElement("hue", 0));

        return data;
    }

    public BsonDocument CreateCharacter(string name) {
        var collection = database.GetCollection<BsonDocument>("characters");
        //hack
        var collection2 = database.GetCollection<BsonDocument>("players");
        var found = collection2.Find(new BsonDocument {{"login", name}} ).FirstOrDefault();

        var data = getDefaultData(name, found);
        collection.InsertOne(data);
        return data;
    }

    public Character GetCharacter(string name) {
        var collection = database.GetCollection<BsonDocument>("characters");
        var found = collection.Find(new BsonDocument {{"name", name}} ).FirstOrDefault();

        if (found == null) {
            found = CreateCharacter(name);
        } else {
            var def = getDefaultData();

            foreach (var element in def) {
                if (found.GetValue(element.Name, false) == false) {
                    found.SetElement(element);
                }
            }
        }

        return new Character(found, this); //może powinno trzymać gdzieś instancje
    }

    public BsonDocument GetCharacterData(Character character) {
        var collection = database.GetCollection<BsonDocument>("characters");
        return collection.Find(new BsonDocument {{"name", character.GetName()}} ).FirstOrDefault();
    }

    public void SaveCharacter(BsonDocument data) {
        GD.Print("Saving ", data.GetValue("name").AsString);
        var collection = database.GetCollection<BsonDocument>("characters");

        var filter = Builders<BsonDocument>.Filter.Eq("_id", data.GetValue("_id").AsObjectId);
        collection.ReplaceOneAsync(filter, data);
    }
}