using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;


[BsonIgnoreExtraElements]
public class RewardCycleModel: BaseModel
{
    //use bson element to map lowercase to camelcase
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("currentEraIndex")] public int CurrentEraIndex { get; set; }

    [BsonElement("startEraIndex")] public int StartEraIndex { get; set; }

    [BsonElement("endEraIndex")] public int EndEraIndex { get; set; }

    [BsonElement("startBlock")] public int StartBlock { get; set; }

    [BsonElement("endBlock")] public int EndBlock { get; set; }

    [BsonElement("calculationComplete")] public bool CalculationComplete { get; set; }

    // Used by validator to determine if work points have been calculated yet
    [BsonElement("calculateWorkPoint")] public bool CalculateWorkPoint { get; set; }
    // Used by validator to determine if points have been submitted
    [BsonElement("registerPointsOnChain")] public bool RegisterPointsOnChain { get; set; }

    [BsonElement("bootstrapRewardInTotal")]
    public string? BootstrapRewardInTotal { get; set; }

    [BsonElement("workpointsRewardInTotal")]
    public string? WorkpointsRewardInTotal { get; set; }

    [BsonElement("vtxDistributionId")] public int VtxDistributionId { get; set; }
}