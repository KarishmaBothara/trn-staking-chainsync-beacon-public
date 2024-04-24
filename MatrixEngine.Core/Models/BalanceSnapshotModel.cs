using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

public class BalanceSnapshotModel
{
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("account")] public string? Account { get; set; }

    [BsonElement("endBlock")] public int EndBlock { get; set; }

    [BsonElement("balance")] public string? Balance { get; set; }

    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; }
}