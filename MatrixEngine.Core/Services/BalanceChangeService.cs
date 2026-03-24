using System.Numerics;
using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.DTOs;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IBalanceChangeService
{
    Task<List<BalanceModel>> GetBalanceChangesInRange(int startBlock, int endBlock);
    Task UpsertUserBalanceChanges(List<BalanceChangeModel> changes);

    Task<List<BalanceModel>> GetUsersLastBalanceChanges(int startBlock, int endBlock);
}

public class BalanceChangeService : IBalanceChangeService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<BalanceChangeService> _logger;

    private IMongoCollection<BalanceModel> Collection =>
        _database.GetCollection<BalanceModel>(DbCollectionName.Balance);

    public BalanceChangeService(IMongoDatabase database, ILogger<BalanceChangeService> logger)
    {
        _logger = logger;
        _database = database;
    }

    // Get all balance changes within a block range
    // Used when calculating reward cycles
    public async Task<List<BalanceModel>> GetBalanceChangesInRange(int startBlock, int endBlock)
    {
        var filter = Builders<BalanceModel>.Filter.Gte(x => x.StartBlock, startBlock) &
                     Builders<BalanceModel>.Filter.Lte(x => x.EndBlock, endBlock);

        var balanceChanges = await Collection.Find(filter).ToListAsync();
        _logger.LogInformation($"Found {balanceChanges.Count} balance changes between blocks {startBlock} and {endBlock}");

        return balanceChanges;
    }

    public async Task UpsertUserBalanceChanges(List<BalanceChangeModel> changes)
    {
        _logger.LogInformation($"Upserting {changes.Count} balance changes");
        const int pageSize = Pagination.DefaultDbPageSize;
        ;
        var totalPages = changes.Count / pageSize + 1;

        for (var pageNumber = 0; pageNumber < totalPages; pageNumber++)
        {
            _logger.LogInformation($"Bulk Upserting page {pageNumber} of {totalPages}.");

            var batch = changes.Skip(pageNumber * pageSize).Take(pageSize).ToList();
            if (batch.Count == 0) break;

            //bulk upsert the data
            var bulkOps = new List<UpdateOneModel<BalanceModel>>();
            foreach (var change in batch)
            {
                var blocks = change.EndBlock - change.StartBlock + 1;
                var filter = Builders<BalanceModel>.Filter.Eq(x => x.Account, change.Account) &
                             Builders<BalanceModel>.Filter.Eq(x => x.StartBlock, change.StartBlock) &
                             Builders<BalanceModel>.Filter.Lte(x => x.EndBlock, change.EndBlock);

                var update = Builders<BalanceModel>.Update
                    .SetOnInsert(x => x.Account, change.Account)
                    .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow)
                    .Set(x => x.StartBlock, change.StartBlock)
                    .Set(x => x.EndBlock, change.EndBlock)
                    .Set(x => x.Bonded, new BalanceDetail(change.Bonded))
                    .Set(x => x.Unlocking, new BalanceDetail(change.Unlocking));

                var model = new UpdateOneModel<BalanceModel>(filter, update) { IsUpsert = true };
                bulkOps.Add(model);
            }

            _logger.LogInformation($"Bulk upserting {bulkOps.Count} balance changes");
            await Collection.BulkWriteAsync(bulkOps);
        }
    }


    public async Task<List<BalanceModel>> GetUsersLastBalanceChanges(int startBlock, int endBlock)
    {
        try
        {
            //this function is to use aggregation to group by account
            //and query the largest block number between startBlock and endBlock
            //use BsonDocument to build aggregation pipeline
            //implementation as above description
            var pipeline = new BsonDocument[]
            {
                new BsonDocument("$match",
                    new BsonDocument
                    {
                        { "startBlock", new BsonDocument("$gte", startBlock) },
                        { "endBlock", new BsonDocument("$lte", endBlock) }
                    }
                ),
                new BsonDocument("$sort",
                    new BsonDocument("endBlock", -1)
                ),
                new BsonDocument("$group",
                    new BsonDocument
                    {
                        { "_id", "$account" },
                        { "latestBalanceDoc", new BsonDocument("$first", "$$ROOT") }
                    }
                ),
                new BsonDocument("$replaceRoot",
                    new BsonDocument("newRoot", "$latestBalanceDoc")
                ),
                new BsonDocument("$project",
                    new BsonDocument("_id", 0))
            };

            var results = await Collection.Aggregate<BalanceModel>(pipeline).ToListAsync();

            return results;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return new List<BalanceModel>();
        }
    }
}