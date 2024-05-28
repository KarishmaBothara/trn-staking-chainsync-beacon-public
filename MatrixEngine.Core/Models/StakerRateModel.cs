using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

[BsonIgnoreExtraElements]
public class StakerRateModel: BaseModel
{
    [BsonId] public ObjectId Id { get; set; }
    
    [BsonElement("account")] public string? Account { get; set; }
    
    [BsonElement("eraIndex")] public int EraIndex { get; set; }
    
    [BsonElement("type")] public string? Type { get; set; }
    
    [BsonElement("rate")] public string? Rate { get; set; }
    
    [BsonElement("signature")] public string? Signature { get; set; }
    
    [BsonElement("timestamp")] public long Timestamp { get; set; }
    
    [BsonElement("batchNumber")] public string? BatchNumber { get; set; }
}