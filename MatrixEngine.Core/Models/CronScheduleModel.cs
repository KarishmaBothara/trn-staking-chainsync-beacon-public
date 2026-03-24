using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

public class CronScheduleModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("cron")]
    public string Name { get; set; }

    [BsonElement("reset")]
    public bool Reset { get; set; }

    [BsonElement("cronTime")]
    public string CronTime { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
} 