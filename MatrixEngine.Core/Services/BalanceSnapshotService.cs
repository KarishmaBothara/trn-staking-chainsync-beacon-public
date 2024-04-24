using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IBalanceSnapshotService
{
    Task<BalanceSnapshotModel> GetBalanceSnapshotByAccount(string account);
    Task<List<BalanceSnapshotModel>> GetBalanceSnapshotByEndBlock(int endBlock);
    Task<bool> HasCycleHaveBaseBalance(RewardCycleModel cycle);

    /// <summary>
    /// Upsert multiple balance snapshots
    /// </summary>
    /// <param name="balanceSnapshots"></param>
    Task UpsertBalanceSnapshots(List<BalanceSnapshotModel> balanceSnapshots);
}

public class BalanceSnapshotService : IBalanceSnapshotService
{
    private readonly IMongoDatabase _database;

    public BalanceSnapshotService(IMongoDatabase database)
    {
        _database = database;
    }

    private IMongoCollection<BalanceSnapshotModel> Collection =>
        _database.GetCollection<BalanceSnapshotModel>(DbCollection.BalanceSnapshot);

    public async Task<BalanceSnapshotModel> GetBalanceSnapshotByAccount(string account)
    {
        var filter = Builders<BalanceSnapshotModel>.Filter.Eq(x => x.Account, account);

        var sort = Builders<BalanceSnapshotModel>.Sort.Descending(x => x.EndBlock);

        var balanceSnapshot = await Collection.Find(filter).Sort(sort).FirstOrDefaultAsync();

        return balanceSnapshot;
    }

    public async Task<List<BalanceSnapshotModel>> GetBalanceSnapshotByEndBlock(int endBlock)
    {
        var filter =
            Builders<BalanceSnapshotModel>.Filter.Eq(x => x.EndBlock, endBlock);

        var balanceSnapshots = await Collection.Find(filter).ToListAsync();

        return balanceSnapshots;
    }

    /// <summary>
    /// Check if the cycle has base balance (balance snapshot) 
    /// </summary>
    /// <param name="cycle"></param>
    /// <returns></returns>
    public async Task<bool> HasCycleHaveBaseBalance(RewardCycleModel cycle)
    {
        var filter = Builders<BalanceSnapshotModel>.Filter.Eq(x => x.EndBlock, cycle.StartBlock - 1);

        return await Collection.CountDocumentsAsync(filter) > 0;
    }

    public async Task BuildCycleBalanceSnapshot(RewardCycleModel completedCycle)
    {
        //1. get base balance
        //  check if the reward cycle is the first one
        //      if yes,
        //          there is no balance snapshot but need to use genesis validator balance as base balance
        //      if no,
        //          get balance snapshot of the last block of the previous cycle
        //          use previous cycle balance snapshot as base balance
        //2. fetch all balance changes(or events) in the latest completed cycle
        //3. use base balance and balance changes to compute the new balance snapshot of the latest completed cycle 
    }

    /// <summary>
    /// Upsert multiple balance snapshots
    /// </summary>
    /// <param name="balanceSnapshots"></param>
    public async Task UpsertBalanceSnapshots(List<BalanceSnapshotModel> balanceSnapshots)
    {
        var ops = balanceSnapshots.Select(s =>
        {
            var filter = Builders<BalanceSnapshotModel>.Filter.Eq(x => x.Account, s.Account) &
                         Builders<BalanceSnapshotModel>.Filter.Eq(x => x.EndBlock, s.EndBlock);
            var update = Builders<BalanceSnapshotModel>.Update
                .SetOnInsert(x => x.Account, s.Account)
                .SetOnInsert(x => x.EndBlock, s.EndBlock)
                .Set(x => x.Balance, s.Balance);
            return new UpdateOneModel<BalanceSnapshotModel>(filter, update) { IsUpsert = true };
        });

        await Collection.BulkWriteAsync(ops.ToList());
    }
}