using System.Numerics;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

// Effective balance per account over one reward cycle
public class EffectiveBalanceModel: BaseModel
{
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("account")] public string? Account { get; set; }

    // Which reward cycle this effective balance is for
    [BsonElement("vtxDistributionId")] public int VtxDistributionId { get; set; }
    
    // Start block of the reward cycle
    [BsonElement("startBlock")] public int StartBlock { get; set; }

    // End block of the reward cycle
    [BsonElement("endBlock")] public int EndBlock { get; set; }
    
    [BsonElement("effectiveBlocks")] public int EffectiveBlocks { get; set; }

    // Percentage of reward cycle that this balance covers
    [BsonElement("percentage")] public double Percentage { get; set; }

    // What reward points will get paid out for this range, sum of bonded and unlocking balances
    [BsonElement("totalRewardPoints")] public string? TotalRewardPoints { get; set; }

    // The bonded portion of their balance
    [BsonElement("bonded")] public EffectiveBalanceDetail Bonded { get; set; }
    
    // The unlocking portion of their balance
    [BsonElement("unlocking")] public EffectiveBalanceDetail Unlocking { get; set; }
}

// Balance details for bonded and unlocking balances
public class EffectiveBalanceDetail
{
    // The actual balance in this block range. Not necessarily representative of the total payout 
    [BsonElement("balance")] public string Balance { get; set; }
    // The effective balance being paid out, different to balance at end of the reward cycle, this takes into account
    // the calculation where we only count balance that made it to the end
    [BsonElement("effectiveBalance")] public string EffectiveBalance { get; set; }
    // The rate of the balance portion, this is the rate that will be used to calculate the reward points
    [BsonElement("rate")] public decimal Rate { get; set; }
    // The reward points that will be paid out for this balance
    [BsonElement("rewardPoints")] public string RewardPoints { get; set; }
    [BsonElement("stakerType")] public string? StakerType { get; set; }
    
    public EffectiveBalanceDetail(BigInteger balance, BigInteger effectiveBalance, decimal rate, BigInteger rewardPoints, string? stakerType)
    {
        Balance = balance.ToString();
        EffectiveBalance = effectiveBalance.ToString();
        Rate = rate;
        RewardPoints = rewardPoints.ToString();
        StakerType = stakerType;
    }
}
