using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

public abstract class BaseModel
{
    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; }
    [BsonElement("updatedAt")] public DateTime UpdatedAt { get; set; }
}