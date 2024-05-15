using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

[BsonIgnoreExtraElements]
public class EraModel: BaseModel
{
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("eraIndex")] public int EraIndex { get; set; }

    [BsonElement("startBlock")] public int StartBlock { get; set; }

    [BsonElement("endBlock")] public int EndBlock { get; set; }
}