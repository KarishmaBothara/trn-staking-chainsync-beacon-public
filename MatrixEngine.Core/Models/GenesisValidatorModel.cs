using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

[BsonIgnoreExtraElements]
public class GenesisValidatorModel
{
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("stash")] public string? Stash { get; set; }

    [BsonElement("lockedBalance")] public string? LockedBalance { get; set; }
}