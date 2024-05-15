using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Exceptions;
using MatrixEngine.Core.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IRewardCycleService
{
    Task<RewardCycleModel> GetCurrentRewardCycle();
    Task<Boolean> IsRewardCycleTheFirstCycle(int startBlock);
    Task<RewardCycleModel> GetRewardCycleByEndBlock(int endBlock);
    Task UpdateRewardCycle(RewardCycleModel rewardCycleModel);
    Task CreateRewardCycle(RewardCycleModel newRewardCycle);
    Task UpdateCurrentEraIndexOfRewardCycle(RewardCycleModel updateModel);
    Task SetNeedToCalculate(RewardCycleModel currentRewardCycle);
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
        var filter = Builders<RewardCycleModel>.Filter.Eq(x => x.Finished, false);

        var rewardCycle = await Collection.Find(filter)
            .SortByDescending(x => x.CreatedAt).FirstOrDefaultAsync();

        if (rewardCycle == null)
        {
            _logger.LogError("No active reward cycle found");
            throw new RewardCycleException("No active reward cycle found");
        }

        return rewardCycle;
    }

    public async Task<Boolean> IsRewardCycleTheFirstCycle(int startBlock)
    {
        var filter = Builders<RewardCycleModel>.Filter.Lt(x => x.StartBlock, startBlock);

        var count = await Collection.CountDocumentsAsync(filter);

        return count == 0;
    }

    public async Task<RewardCycleModel> GetRewardCycleByEndBlock(int endBlock)
    {
        var filter = Builders<RewardCycleModel>.Filter.Eq(x => x.EndBlock, endBlock);

        var rewardCycle = await Collection.Find(filter).FirstOrDefaultAsync();

        return rewardCycle;
    }

    public async Task UpdateRewardCycle(RewardCycleModel rewardCycleModel)
    {
        var filter = Builders<RewardCycleModel>.Filter.Eq(x => x.StartBlock, rewardCycleModel.StartBlock);
        var update = Builders<RewardCycleModel>.Update
            .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.EndBlock, rewardCycleModel.EndBlock)
            .Set(x => x.Finished, rewardCycleModel.Finished)
            .Set(x => x.CurrentEraIndex, rewardCycleModel.CurrentEraIndex)
            .Set(x => x.EndEraIndex, rewardCycleModel.EndEraIndex);

        await Collection.UpdateOneAsync(filter, update);
    }

    public async Task CreateRewardCycle(RewardCycleModel newRewardCycle)
    {
        var filter = Builders<RewardCycleModel>.Filter.Eq(x => x.StartBlock, newRewardCycle.StartBlock) &
                     Builders<RewardCycleModel>.Filter.Eq(x => x.StartEraIndex, newRewardCycle.StartEraIndex);

        var update = Builders<RewardCycleModel>.Update.Set(x => x.StartBlock, newRewardCycle.StartBlock)
            .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.StartEraIndex, newRewardCycle.StartEraIndex)
            .Set(x => x.Finished, newRewardCycle.Finished)
            .Set(x => x.CurrentEraIndex, newRewardCycle.CurrentEraIndex);

        await Collection.UpdateOneAsync(filter, update, new UpdateOptions() { IsUpsert = true });
    }

    public async Task UpdateCurrentEraIndexOfRewardCycle(RewardCycleModel updateModel)
    {
        var filter = Builders<RewardCycleModel>.Filter.Eq(x => x.StartBlock, updateModel.StartBlock) &
                     Builders<RewardCycleModel>.Filter.Eq(x => x.StartEraIndex, updateModel.StartEraIndex);

        var update = Builders<RewardCycleModel>.Update
            .Set(x => x.CurrentEraIndex, updateModel.CurrentEraIndex)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await Collection.UpdateOneAsync(filter, update);
    }

    public async Task SetNeedToCalculate(RewardCycleModel currentRewardCycle)
    {
        _filterDefinitionBuilder = Builders<RewardCycleModel>.Filter;
        var filter = _filterDefinitionBuilder.Eq(x => x.StartBlock, currentRewardCycle.StartBlock) &
                     _filterDefinitionBuilder.Eq(x => x.EndBlock, currentRewardCycle.EndBlock);
        
        var update = Builders<RewardCycleModel>.Update.Set(x => x.NeedToCalculate, true);
        
        await Collection.UpdateOneAsync(filter, update);
    }
}