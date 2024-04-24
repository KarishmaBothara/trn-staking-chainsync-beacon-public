using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IBalanceChangeService
{
    Task<List<BalanceModel>> GetBalanceChanges(string account, int startBlock, int endBlock);
    Task UpsertUserBalanceChanges(List<BalanceChangeModel> changes);
}

public class BalanceChangeService : IBalanceChangeService
{
    private readonly IMongoDatabase _database;
    private ILogger<BalanceChangeService> _logger;

    private IMongoCollection<BalanceModel> Collection => _database.GetCollection<BalanceModel>(DbCollection.Balance);

    public BalanceChangeService(IMongoDatabase database, ILogger<BalanceChangeService> logger)
    {
        _logger = logger;
        _database = database;
    }

    public async Task<List<BalanceModel>> GetBalanceChanges(string account, int startBlock, int endBlock)
    {
        var filter = Builders<BalanceModel>.Filter.Eq(x => x.Account, account) &
                     Builders<BalanceModel>.Filter.Eq(x => x.StartBlock, startBlock) &
                     Builders<BalanceModel>.Filter.Eq(x => x.EndBlock, endBlock);

        var balanceChanges = await Collection.Find(filter).ToListAsync();
        _logger.LogInformation(
            $"Found {balanceChanges.Count} balance changes for account {account} between blocks {startBlock} and {endBlock}");

        return balanceChanges;
    }

    public async Task UpsertUserBalanceChanges(List<BalanceChangeModel> changes)
    {
        _logger.LogInformation($"Upserting {changes.Count} balance changes");
        //bulk upsert the data
        var bulkOps = new List<UpdateOneModel<BalanceModel>>();

        foreach (var change in changes)
        {
            var blocks = change.EndBlock - change.StartBlock + 1;
            var filter = Builders<BalanceModel>.Filter.Eq(x => x.Account, change.Account) &
                         Builders<BalanceModel>.Filter.Eq(x => x.StartBlock, change.StartBlock) &
                         Builders<BalanceModel>.Filter.Lte(x => x.EndBlock, change.EndBlock) &
                         Builders<BalanceModel>.Filter.Lte(x => x.Blocks, blocks);

            var update = Builders<BalanceModel>.Update
                .SetOnInsert(x => x.Account, change.Account)
                .Set(x => x.StartBlock, change.StartBlock)
                .Set(x => x.EndBlock, change.EndBlock)
                .Set(x => x.Balance, change.BalanceInBlockRange.ToString())
                .Set(x => x.BalanceChange, change.BalanceChange.ToString())
                .Set(x => x.PreviousBalance, change.PreviousBalance.ToString())
                .Set(x => x.Blocks, blocks);

            var model = new UpdateOneModel<BalanceModel>(filter, update) { IsUpsert = true };
            bulkOps.Add(model);
        }

        _logger.LogInformation($"Bulk upserting {bulkOps.Count} balance changes");
        await Collection.BulkWriteAsync(bulkOps);
    }
}