using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.DTOs;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IEffectiveBalanceService
{
    Task UpsertEffectiveBalance(List<EffectiveBalanceModel> data);
    Task RemoveEffectiveBalanceInBlocksRange(int startBlock, int endBlock);
    
    Task<List<EffectiveBalanceModel>> GetPreviousCycleEffectiveBalances(
        int rewardCycleStartBlock
    );
}

public class EffectiveBalanceService : IEffectiveBalanceService
{
    private readonly IMongoDatabase _database;
    private readonly FilterDefinitionBuilder<EffectiveBalanceModel> _filterDef = Builders<EffectiveBalanceModel>.Filter;
    private ILogger<EffectiveBalanceService> _logger;
    private IMongoCollection<EffectiveBalanceModel> Collection =>
        _database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);

    public EffectiveBalanceService(IMongoDatabase database, ILogger<EffectiveBalanceService> logger)
    {
        _logger = logger;
        _database = database;
    }
    
    // Get the effective balances for the previous cycle
    // rewardCycleStartBlock is the start block of the current cycle
    // Used to calculate effective balance of accounts that don't have events in the current cycle
    public async Task<List<EffectiveBalanceModel>> GetPreviousCycleEffectiveBalances(int rewardCycleStartBlock)
    {
        var filter = _filterDef.Eq(x => x.EndBlock, rewardCycleStartBlock - 1);
        var effectiveBalances = await Collection.Find(filter).ToListAsync();
        return effectiveBalances;
    }

    public async Task UpsertEffectiveBalance(List<EffectiveBalanceModel> data)
    {
        _logger.LogInformation($"Upserting {data.Count} effective balances");
        //to reduce db load, page by 500 and insert them

        var pageSize = Pagination.DefaultDbPageSize;
        var totalPages = data.Count / pageSize + 1;

        for (var pageNumber = 0; pageNumber < totalPages; pageNumber++)
        {
            _logger.LogInformation($"Bulk Upserting page {pageNumber} of {totalPages}.");
            var batch = data.Skip(pageNumber * pageSize).Take(pageSize).ToList();
            if(batch.Count == 0) break;
            //bulk upsert data
            var bulkOps = new List<WriteModel<EffectiveBalanceModel>>();
            foreach (var item in batch)
            {
                var filter = _filterDef.Eq(x => x.Account, item.Account) &
                             _filterDef.Eq(x => x.StartBlock, item.StartBlock) &
                             _filterDef.Eq(x => x.EndBlock, item.EndBlock);

                var update = Builders<EffectiveBalanceModel>.Update
                    .SetOnInsert(x => x.Account, item.Account)
                    .SetOnInsert(x => x.VtxDistributionId, item.VtxDistributionId)
                    .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow)
                    .Set(x => x.StartBlock, item.StartBlock)
                    .Set(x => x.EndBlock, item.EndBlock)
                    .Set(x => x.EffectiveBlocks, item.EffectiveBlocks)
                    .Set(x => x.Percentage, item.Percentage)
                    .Set(x => x.TotalRewardPoints, item.TotalRewardPoints)
                    .Set(x => x.Bonded, item.Bonded)
                    .Set(x => x.Unlocking, item.Unlocking);

                var upsertOne = new UpdateOneModel<EffectiveBalanceModel>(filter, update) { IsUpsert = true };
                bulkOps.Add(upsertOne);
            }

            await Collection.BulkWriteAsync(bulkOps);
        }
    }

    public async Task RemoveEffectiveBalanceInBlocksRange(int startBlock, int endBlock)
    {
        var filter = _filterDef.And(_filterDef.Gte(x => x.StartBlock, startBlock)
                                    & _filterDef.Lte(x => x.EndBlock, endBlock));

        await Collection.DeleteManyAsync(filter);
    }
}