using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models.Events;

public static class TransactionType
{
    public const string Bonded = "bonded";
    public const string Withdrawn = "withdrawn"; 
}

[BsonIgnoreExtraElements]
public class TransactionModel
{
    [BsonId] public ObjectId Id { get; set; }
    [BsonElement("account")] public string Account { get; set; }
    [BsonElement("amount")] public string Amount { get; set; }
    [BsonElement("blockNumber")] public int BlockNumber { get; set; }
    [BsonElement("type")] public string Type { get; set; }
}