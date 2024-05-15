using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

public class EffectiveBalanceModel: BaseModel
{
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("account")] public string? Account { get; set; }

    [BsonElement("eraIndex")] public int EraIndex { get; set; }
    
    [BsonElement("startBlock")] public int StartBlock { get; set; }

    [BsonElement("endBlock")] public int EndBlock { get; set; }

    [BsonElement("balance")] public string? Balance { get; set; }

    [BsonElement("effectiveBalance")] public string? EffectiveBalance { get; set; }

    [BsonElement("effectiveBlocks")] public int EffectiveBlocks { get; set; }

    [BsonElement("percentage")] public double Percentage { get; set; }

    [BsonElement("rate")] public decimal Rate { get; set; }

    [BsonElement("reward")] public string? Reward { get; set; }

    [BsonElement("type")] public string? Type { get; set; }

    [BsonElement("effectiveEras")] public decimal EffectiveEras { get; set; }
}