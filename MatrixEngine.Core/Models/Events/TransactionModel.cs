using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models.Events;

public static class TransactionType
{
    public const string Bonded = "bonded";
    public const string ReBonded = "rebonded";
    public const string Unbonded = "unbonded";
    public const string Withdrawn = "withdrawn";
    public const string Slashed = "slashed";
    // Used internally to indicate a transaction that came from the previous cycle
    public const string Carryover = "carryover"; 
}

[BsonIgnoreExtraElements]
public class TransactionModel: BaseModel
{
    [BsonId] public ObjectId Id { get; set; }
    [BsonElement("account")] public string Account { get; set; }
    [BsonElement("amount")] public string Amount { get; set; }
    [BsonElement("blockNumber")] public int BlockNumber { get; set; }
    [BsonElement("type")] public string Type { get; set; }
}