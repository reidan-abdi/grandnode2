// See https://aka.ms/new-console-template for more information

using MongoDB.Bson;
using MongoDB.Driver;

var client = new MongoClient("mongodb://root:example@localhost:27018/grandnodedb2?authSource=admin");
var database = client.GetDatabase("grandnodedb2");
database.RunCommand((Command<BsonDocument>)"{ping:1}");
// database.CreateCollection("temp2");
Console.WriteLine("Hello, World!");