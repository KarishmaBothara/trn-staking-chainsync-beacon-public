using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Exceptions;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.DTOs;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IRewardCycleService
{
    Task<RewardCycleModel?> GetCurrentRewardCycle();
    Task CreateRewardCycle(RewardCycleModel newRewardCycle);
    Task UpdateCurrentEraIndexOfRewardCycle(int vtxDistId, int currentEraIndex);
    Task UpdateRewardCycleToComplete(RewardCycle rewardCycle, int endBlock);
    Task UpdateRewardCycleCurrentEra(RewardCycle rewardCycle, int currentEra);

}

public class RewardCycleService : IRewardCycleService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<RewardCycleService> _logger;
    private FilterDefinitionBuilder<RewardCycleModel>? _filterDefinitionBuilder;

    public RewardCycleService(IMongoDatabase database, ILogger<RewardCycleService> logger)
    {
        _logger = logger;
        _database = database;
    }

    private IMongoCollection<RewardCycleModel> Collection =>
        _database.GetCollection<RewardCycleModel>(DbCollectionName.RewardCycle);

    public async Task<RewardCycleModel> GetCurrentRewardCycle()
    {
        _logger.LogInformation("GetCurrentRewardCycle");
        var filter = Builders<RewardCycleModel>.Filter.Eq(x => x.CalculationComplete, false);
        var rewardCycle = await Collection.Find(filter).FirstOrDefaultAsync();
        return rewardCycle;
    }

    // Create a brand new reward cycle and save to DB
    public async Task CreateRewardCycle(RewardCycleModel newRewardCycle)
    {
        var filter = Builders<RewardCycleModel>.Filter
            .Eq(x => x.VtxDistributionId, newRewardCycle.VtxDistributionId);

        var update = Builders<RewardCycleModel>.Update.SetOnInsert(x => x.StartBlock, newRewardCycle.StartBlock)
            .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow)
            .SetOnInsert(x => x.VtxDistributionId, newRewardCycle.VtxDistributionId)
            .Set(x => x.EndBlock, newRewardCycle.EndBlock)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.StartEraIndex, newRewardCycle.StartEraIndex)
            .Set(x => x.EndEraIndex, newRewardCycle.EndEraIndex)
            .Set(x => x.CalculationComplete, newRewardCycle.CalculationComplete)
            .Set(x => x.CurrentEraIndex, newRewardCycle.CurrentEraIndex)
            .Set(x => x.CalculateWorkPoint, false)
            .Set(x => x.RegisterPointsOnChain, false);

        await Collection.UpdateOneAsync(filter, update, new UpdateOptions() { IsUpsert = true });
    }
    

    // Updates the current era index of the reward cycle, this is called when the reward cycle is updated but not complete
    public async Task UpdateCurrentEraIndexOfRewardCycle(int vtxDistId, int currentEraIndex)
    {
        var filter = Builders<RewardCycleModel>.Filter.Eq(x => x.VtxDistributionId, vtxDistId);
        
        var update = Builders<RewardCycleModel>.Update
            .Set(x => x.CurrentEraIndex, currentEraIndex)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await Collection.UpdateOneAsync(filter, update);
    }

    // Called once a reward cycle has been completely calculated and the current era is beyond the end era
    // Sets Finished to true
    // Sets NeedToCalculate to true
    // Sets the final end block of the reward cycle, up until this point the end block will be unknown
    public async Task UpdateRewardCycleToComplete(RewardCycle rewardCycle, int endBlock)
    {
        var filter = Builders<RewardCycleModel>.Filter
            .Eq(x => x.VtxDistributionId, rewardCycle.VtxDistributionId);
        
        var update = Builders<RewardCycleModel>.Update
            .Set(x => x.CalculationComplete, true)
            .Set(x => x.EndBlock, endBlock)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.EndEraIndex, rewardCycle.EndEraIndex)
            .Set(x => x.CurrentEraIndex, rewardCycle.EndEraIndex);
        
        await Collection.UpdateOneAsync(filter, update);
    }

    // After processing a reward cycle, we need to update the current block of the reward cycle to the last processed era
    public async Task UpdateRewardCycleCurrentEra(RewardCycle rewardCycle, int currentEra)
    {
        var filter = Builders<RewardCycleModel>.Filter
            .Eq(x => x.VtxDistributionId, rewardCycle.VtxDistributionId);
        
        var update = Builders<RewardCycleModel>.Update
            .Set(x => x.EndBlock, -1) // Set to -1 as the cycle is not finished
            .Set(x => x.EndEraIndex, -1) // Set to -1 as the cycle is not finished
            .Set(x => x.CurrentEraIndex, currentEra)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        await Collection.UpdateOneAsync(filter, update);
    }
}