using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

[BsonIgnoreExtraElements]
public class SignedEffectiveBalanceModel : BaseModel
{
    [BsonElement("account")] public string? Account { get; set; }
    [BsonElement("totalRewardPoints")] public string? TotalRewardPoints { get; set; }
    [BsonElement("startBlock")] public int StartBlock { get; set; }
    [BsonElement("endBlock")] public int EndBlock { get; set; }
    [BsonElement("vtxDistributionId")] public int VtxDistributionId { get; set; }
//     [BsonElement("signature")] public string? Signature { get; set; }
    [BsonElement("timestamp")] public long Timestamp  { get; set; }
    // Validator has verified the signature
    [BsonElement("verified")] public bool Verified { get; set; } = false;
    // Has the effective balance been submitted to the chain
    [BsonElement("submitted")] public bool Submitted { get; set; } = false;
}
