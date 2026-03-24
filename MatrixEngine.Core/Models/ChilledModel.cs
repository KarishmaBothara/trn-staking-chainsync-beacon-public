using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

// ChilledModel represents all chilled events for accounts at a given block number
[BsonIgnoreExtraElements]
public class ChilledModel: BaseModel
{
    [BsonId] public ObjectId Id { get; set; }
    // Stash account that was chilled
    [BsonElement("account")] public string? Account { get; set; }
    // Block number when the chilled event occured
    [BsonElement("blockNumber")] public int BlockNumber { get; set; }
}