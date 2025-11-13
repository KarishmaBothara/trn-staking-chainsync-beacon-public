using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MatrixEngine.Core.Models;

/// <summary>
/// This model is used to store the withdrawn transactions which apply the punishment to the effective balance.
/// Every calculation will store the withdrawn transactions
/// and use them to find which account and era index need to resubmit effective balance to chain.
/// However, as this is the transit data, it will be removed after each calculation.
/// </summary>
[BsonIgnoreExtraElements]
public class AccountPunishmentMarkModel : BaseModel
{
    //use bson element to map lowercase to camelcase
    [BsonId] public ObjectId Id { get; set; }
    
    [BsonElement("account")] public string? Account { get; set; }
    
    [BsonElement("blockNumber")] public int BlockNumber { get; set; }
    
    [BsonElement("amount")] public string? Amount { get; set; }
    
    [BsonElement("type")] public string? Type { get; set; }

    
    [BsonElement("applied")] public bool Applied { get; set; } = false;
}