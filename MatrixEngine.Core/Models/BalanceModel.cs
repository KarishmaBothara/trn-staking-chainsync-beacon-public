using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

[BsonIgnoreExtraElements]
public class BalanceModel: BaseModel
{
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("account")] public string? Account { get; set; }

    [BsonElement("startBlock")] public int StartBlock { get; set; }

    [BsonElement("endBlock")] public int EndBlock { get; set; }

    [BsonElement("blocks")] public int Blocks { get; set; }

    [BsonElement("balance")] public string? Balance { get; set; }
    
    [BsonElement("balanceChange")] public string? BalanceChange { get; set; }
    
    [BsonElement("previousBalanceChange")] public string? PreviousBalance { get; set; }
}