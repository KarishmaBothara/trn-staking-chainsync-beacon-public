using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IBalanceSnapshotService
{
    Task<List<BalanceSnapshotModel>> GetBalanceSnapshotByCycleEndBlock(int cycleEndBlock);
    Task<bool> HasCycleHaveBaseBalance(int cycleStartBlock);

    /// <summary>
    /// Upsert multiple balance snapshots
    /// </summary>
    /// <param name="balanceSnapshots"></param>
    /// <param name="once"></param>
    Task UpsertBalanceSnapshots(List<BalanceSnapshotModel> balanceSnapshots);
}

public class BalanceSnapshotService : IBalanceSnapshotService
{
    private readonly IMongoDatabase _database;
    private ILogger<BalanceSnapshotService> _logger;

    public BalanceSnapshotService(IMongoDatabase database, ILogger<BalanceSnapshotService> logger)
    {
        _logger = logger;
        _database = database;
    }

    private IMongoCollection<BalanceSnapshotModel> Collection =>
        _database.GetCollection<BalanceSnapshotModel>(DbCollectionName.BalanceSnapshot);


    public async Task<List<BalanceSnapshotModel>> GetBalanceSnapshotByCycleEndBlock(int cycleEndBlock)
    {
        var filter =
            Builders<BalanceSnapshotModel>.Filter.Eq(x => x.EndBlock, cycleEndBlock);

        var balanceSnapshots = await Collection.Find(filter).ToListAsync();

        return balanceSnapshots;
    }

    /// <summary>
    /// Check if the cycle has base balance (balance snapshot) 
    /// </summary>
    /// <param name="cycleStartBlock"></param>
    /// <returns></returns>
    public async Task<bool> HasCycleHaveBaseBalance(int cycleStartBlock)
    {
        var filter = Builders<BalanceSnapshotModel>.Filter.Eq(x => x.EndBlock, cycleStartBlock - 1);

        return await Collection.CountDocumentsAsync(filter) > 0;
    }

    /// <summary>
    /// Upsert multiple balance snapshots
    /// </summary>
    /// <param name="balanceSnapshots"></param>
    /// <param name="once"></param>
    public async Task UpsertBalanceSnapshots(List<BalanceSnapshotModel> balanceSnapshots)
    {
        _logger.LogInformation($"Upserting {balanceSnapshots.Count} balance snapshots.");
        //to reduce db load, page by 500 and insert them
        const int pageSize = Pagination.DefaultDbPageSize;;
        var totalPages = balanceSnapshots.Count / pageSize + 1;

        for (var pageNumber = 0; pageNumber < totalPages; pageNumber++)
        {
            _logger.LogInformation($"Upserting page {pageNumber} of {totalPages}.");
            var batch = balanceSnapshots.Skip(pageNumber * pageSize).Take(pageSize).ToList();
            if(batch.Count == 0) break;
            
            var ops = batch.Select(s =>
            {
                var filter = Builders<BalanceSnapshotModel>.Filter.Eq(x => x.Account, s.Account) &
                             Builders<BalanceSnapshotModel>.Filter.Eq(x => x.EndBlock, s.EndBlock);
                var update = Builders<BalanceSnapshotModel>.Update
                    .SetOnInsert(x => x.Account, s.Account)
                    .SetOnInsert(x => x.EndBlock, s.EndBlock)
                    .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow)
                    .Set(x => x.Balance, s.Balance);
                return new UpdateOneModel<BalanceSnapshotModel>(filter, update) { IsUpsert = true };
            });

            await Collection.BulkWriteAsync(ops.ToList());
        }
    }
}