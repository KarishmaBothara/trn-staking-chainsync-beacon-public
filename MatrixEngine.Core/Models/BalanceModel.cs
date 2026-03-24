using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

[BsonIgnoreExtraElements]
public class BalanceModel : BaseModel
{
    [BsonId] public ObjectId Id { get; set; }
    [BsonElement("account")] public string Account { get; set; }
    [BsonElement("startBlock")] public int StartBlock { get; set; }
    [BsonElement("endBlock")] public int EndBlock { get; set; }
    [BsonElement("bonded")] public BalanceDetail Bonded { get; set; }
    [BsonElement("unlocking")] public BalanceDetail Unlocking { get; set; }
}

// Separate from BalanceChangeModel as we need to store values in DB as strings
public class BalanceDetail
{
    public string PreviousBalance { get; set; }
    public string BalanceChange { get; set; }
    public string BalanceInBlockRange { get; set; }
    public string? StakerType { get; set; }
    
    // Constructor for UnlockingBalanceDetail without staker type, defaults to Staker
    public BalanceDetail(BalanceChangeDetail balanceChangeDetail)
    {
        PreviousBalance = balanceChangeDetail.PreviousBalance.ToString();
        BalanceChange = balanceChangeDetail.BalanceChange.ToString();
        BalanceInBlockRange = balanceChangeDetail.BalanceInBlockRange.ToString();
        StakerType = balanceChangeDetail.StakerType;
    }
}
