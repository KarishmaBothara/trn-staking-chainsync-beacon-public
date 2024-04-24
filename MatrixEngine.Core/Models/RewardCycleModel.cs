using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

[BsonIgnoreExtraElements]
public class RewardCycleModel
{
    //use bson element to map lowercase to camelcase
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("currentEraIndex")] public int CurrentEraIndex { get; set; }

    [BsonElement("startEraIndex")] public int StartEraIndex { get; set; }

    [BsonElement("endEraIndex")] public int EndEraIndex { get; set; }

    [BsonElement("startBlock")] public int StartBlock { get; set; }

    [BsonElement("endBlock")] public int EndBlock { get; set; }

    [BsonElement("finished")] public bool Finished { get; set; }

    [BsonElement("needToCalculate")] public bool NeedToCalculate { get; set; }

    [BsonElement("bootstrapRewardInTotal")]
    public string? BootstrapRewardInTotal { get; set; }

    [BsonElement("workpointsRewardInTotal")]
    public string? WorkpointsRewardInTotal { get; set; }

    [BsonElement("vtxDistributionId")] public string? VtxDistributionId { get; set; }

    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; }
}