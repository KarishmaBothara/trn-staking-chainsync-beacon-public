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
    RewardCycleModel GetRewardCycleByEndBlock(int endBlock);
}

public class RewardCycleService : IRewardCycleService
{
    private readonly IMongoDatabase _database;
    private ILogger<RewardCycleService> _logger;

    public RewardCycleService(IMongoDatabase database, ILogger<RewardCycleService> logger)
    {
        _logger = logger;
        _database = database;
    }

    private IMongoCollection<RewardCycleModel> Collection =>
        _database.GetCollection<RewardCycleModel>(DbCollection.RewardCycle);

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

    public RewardCycleModel GetRewardCycleByEndBlock(int endBlock)
    {
        var filter = Builders<RewardCycleModel>.Filter.Eq(x => x.EndBlock, endBlock);

        var rewardCycle = Collection.Find(filter).FirstOrDefault();

        return rewardCycle;
    }
}