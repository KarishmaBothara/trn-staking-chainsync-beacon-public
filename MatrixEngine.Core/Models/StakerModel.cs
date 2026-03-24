using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

[BsonIgnoreExtraElements]
public class StakerModel: BaseModel
{
    [BsonId] public ObjectId Id { get; set; }
    [BsonElement("account")] public string? Account { get; set; }
    [BsonElement("eraIndex")] public int EraIndex { get; set; }
    [BsonElement("type")] public string? Type { get; set; }
    // Actually the "ParentStash" field
    [BsonElement("validatorStash")] public string? ValidatorStash { get; set; }
    [BsonElement("totalStake")] public string? TotalStake { get; set; }
}