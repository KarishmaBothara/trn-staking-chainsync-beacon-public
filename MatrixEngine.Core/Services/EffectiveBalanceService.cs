using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.DTOs;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IEffectiveBalanceService
{
    Task<List<EffectiveBalanceModel>> GetEffectiveBalancesByAccount(string account);
    Task UpsertEffectiveBalance(List<EffectiveBalanceModel> data);
    Task RemoveEffectiveBalanceInBlocksRange(int startBlock, int endBlock);

    Task<List<EffectiveBalanceModel>> LoadAccountEffectiveBalanceInEraRange(string? account, int startEra,
        int endEra);

    Task<(List<AccountRewardWithPercentage> accountsRewardWithPercentage, double totalReward)>
        GetBootstrapRewardInBlockRange(int startBlock, int endBlock);

    Task<double> GetTotalRewardInEffectiveBalanceForCycle(int startBlock, int endBlock);
}

public class EffectiveBalanceService : IEffectiveBalanceService
{
    private readonly IMongoDatabase _database;
    private readonly FilterDefinitionBuilder<EffectiveBalanceModel> _filterDef = Builders<EffectiveBalanceModel>.Filter;
    private ILogger<EffectiveBalanceService> _logger;

    public EffectiveBalanceService(IMongoDatabase database, ILogger<EffectiveBalanceService> logger)
    {
        _logger = logger;
        _database = database;
    }

    private IMongoCollection<EffectiveBalanceModel> Collection =>
        _database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);

    public async Task<List<EffectiveBalanceModel>> GetEffectiveBalancesByAccount(string account)
    {
        var filter = _filterDef.Eq(x => x.Account, account);

        var effectiveBalances = await Collection.Find(filter).ToListAsync();

        return effectiveBalances;
    }

    public async Task UpsertEffectiveBalance(List<EffectiveBalanceModel> data)
    {
        _logger.LogInformation($"Upserting {data.Count} effective balances");
        //to reduce db load, page by 500 and insert them
        
        var pageSize = 500;
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
                    .SetOnInsert(x => x.EraIndex, item.EraIndex)
                    .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow)
                    .Set(x => x.StartBlock, item.StartBlock)
                    .Set(x => x.EndBlock, item.EndBlock)
                    .Set(x => x.Balance, item.Balance)
                    .Set(x => x.EffectiveBalance, item.EffectiveBalance)
                    .Set(x => x.EffectiveBlocks, item.EffectiveBlocks)
                    .Set(x => x.Percentage, item.Percentage)
                    .Set(x => x.Rate, item.Rate)
                    .Set(x => x.Reward, item.Reward)
                    .Set(x => x.Type, item.Type)
                    .Set(x => x.EffectiveEras, item.EffectiveEras);

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

    public Task<List<EffectiveBalanceModel>> LoadAccountEffectiveBalanceInEraRange(string? account, int startEra,
        int endEra)
    {
        //load account effective balance in block range
        var filter = _filterDef.Eq(x => x.Account, account) &
                     _filterDef.And(
                         _filterDef.Gte(x => x.EraIndex, startEra)
                         , _filterDef.Lte(x => x.EraIndex, endEra)
                     );

        return Collection.Find(filter).ToListAsync();
    }

    public async Task<(List<AccountRewardWithPercentage> accountsRewardWithPercentage, double totalReward)>
        GetBootstrapRewardInBlockRange(int startBlock, int endBlock)
    {
        var pipeline = new BsonDocument[]
        {
            new BsonDocument("$match",
                new BsonDocument
                {
                    { "startBlock", new BsonDocument("$gte", startBlock) },
                    { "endBlock", new BsonDocument("$lte", endBlock) }
                }
            ),
            new BsonDocument("$group",
                new BsonDocument
                {
                    { "_id", "$account" },
                    {
                        "totalReward", new BsonDocument("$sum",
                            new BsonDocument("$toDouble", "$reward"))
                    }
                }
            )
        };

        var result = await Collection.AggregateAsync<AccountTotalReward>(pipeline);
        var totalReward = await GetTotalRewardInEffectiveBalanceForCycle(startBlock, endBlock);
        var accountsRewardWithPercentage = result.ToEnumerable().Select(r => new AccountRewardWithPercentage
        {
            Account = r.Account.ToLower(),
            Reward = r.TotalReward,
            Percentage = 100 * r.TotalReward / totalReward
        }).ToList();

        return (accountsRewardWithPercentage, totalReward);
    }

    public async Task<double> GetTotalRewardInEffectiveBalanceForCycle(int startBlock, int endBlock)
    {
        var pipeline = new BsonDocument[]
        {
            new BsonDocument("$match",
                new BsonDocument
                {
                    { "startBlock", new BsonDocument("$gte", startBlock) },
                    { "endBlock", new BsonDocument("$lte", endBlock) }
                }
            ),
            new BsonDocument("$addFields",
                new BsonDocument
                {
                    {
                        "rewardInt",
                        new BsonDocument("$convert",
                            new BsonDocument
                            {
                                { "input", "$reward" },
                                { "to", "double" }
                            }
                        )
                    }
                }
            ),
            new BsonDocument("$group",
                new BsonDocument
                {
                    { "_id", null },
                    {
                        "totalReward",
                        new BsonDocument("$sum", "$rewardInt")
                    }
                }
            )
        };

        var rewards = await Collection.AggregateAsync<BsonDocument>(pipeline);
        var totalReward = rewards.First()["totalReward"].AsDouble;

        return totalReward;
    }
}