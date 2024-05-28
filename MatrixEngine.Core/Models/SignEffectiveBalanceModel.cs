using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

/// <summary>
/// This collection is to store the data for signing.
///  
/// </summary>
[BsonIgnoreExtraElements]
public class SignEffectiveBalanceModel : BaseModel
{
    //TODO: Need to confirm what fields are needed
    [BsonElement("account")] public string? Account { get; set; }
    [BsonElement("eraIndex")] public int EraIndex { get; set; }
    [BsonElement("effectiveBalance")] public string? EffectiveBalance { get; set; }
    [BsonElement("effectiveBlocks")] public int EffectiveBlocks { get; set; }
    [BsonElement("signature")] public string? Signature { get; set; }
    [BsonElement("timestamp")] public long Timestamp  { get; set; }
    [BsonElement("batchNumber")] public string? BatchNumber { get; set; }
    [BsonElement("submitted")] public bool Submitted { get; set; } = false;
}