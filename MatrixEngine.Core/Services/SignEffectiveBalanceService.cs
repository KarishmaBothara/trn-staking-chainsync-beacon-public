using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface ISignEffectiveBalanceService
{
    Task InsertSignEffectiveBalance(List<SignEffectiveBalanceModel> signEffectiveBalanceModels);

    /// <summary>
    /// Load latest unsigned effective balances
    /// </summary>
    /// <returns></returns>
    Task<List<SignEffectiveBalanceModel>> LoadUnsignedEffectiveBalances();

    Task UpdateSignedEffectiveBalance(List<SignEffectiveBalanceModel> batchSignEffectiveBalances);
    Task<int> FindLatestEraIndex(string account);
}

public class SignEffectiveBalanceService : ISignEffectiveBalanceService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<SignEffectiveBalanceService> _logger;

    private readonly FilterDefinitionBuilder<SignEffectiveBalanceModel> filterDef =
        Builders<SignEffectiveBalanceModel>.Filter;

    private IMongoCollection<SignEffectiveBalanceModel> Collection =>
        _database.GetCollection<SignEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);

    public SignEffectiveBalanceService(IMongoDatabase database, ILogger<SignEffectiveBalanceService> logger)
    {
        _logger = logger;
        _database = database;
        
        Collection.Indexes.CreateOne(new CreateIndexModel<SignEffectiveBalanceModel>(
            Builders<SignEffectiveBalanceModel>.IndexKeys.Ascending(x => x.Account)
                .Ascending(x => x.EraIndex)
                .Ascending(x => x.EffectiveBalance)
        ));
    }

    public async Task InsertSignEffectiveBalance(List<SignEffectiveBalanceModel> signEffectiveBalanceModels)
    {
        // bulk write to insert data
        _logger.LogInformation($"Inserting {signEffectiveBalanceModels.Count} ready to sign effective balances.");

        //to reduce db load, page by 500 and insert them
        const int pageSize = Pagination.DefaultDbPageSize;
        ;
        var totalPages = signEffectiveBalanceModels.Count / pageSize + 1;

        for (var pageNumber = 0; pageNumber < totalPages; pageNumber++)
        {
            _logger.LogInformation($"Inserting page {pageNumber} of {totalPages}.");

            var batch = signEffectiveBalanceModels.Skip(pageNumber * pageSize).Take(pageSize).ToList();
            if (batch.Count == 0) break;

            var ops = batch.Select(x =>
            {
                var filter = filterDef.Eq(f => f.Account, x.Account) &
                             filterDef.Eq(f => f.EraIndex, x.EraIndex) & 
                             filterDef.Eq(f => f.EffectiveBalance, x.EffectiveBalance);

                var update = Builders<SignEffectiveBalanceModel>.Update
                    .SetOnInsert(u => u.CreatedAt, DateTime.UtcNow)
                    .Set(u => u.Account, x.Account)
                    .Set(u => u.EffectiveBalance, x.EffectiveBalance)
                    .Set(u => u.EraIndex, x.EraIndex)
                    .Set(u => u.EffectiveBlocks, x.EffectiveBlocks)
                    .Set(u => u.UpdatedAt, DateTime.UtcNow);
                return new UpdateOneModel<SignEffectiveBalanceModel>(filter, update) { IsUpsert = true };
            });

            await Collection.BulkWriteAsync(ops);
        }
    }

    public async Task<List<SignEffectiveBalanceModel>> LoadUnsignedEffectiveBalances()
    {
        //load all unsigned effective balances with conditions that data has no timestamp and no signature added
        var filterDefinition = Builders<SignEffectiveBalanceModel>.Filter;

        var filter = filterDefinition.Eq(x => x.BatchNumber, null) &
                     filterDefinition.Eq(x => x.Signature, null) &
                     filterDefinition.Or(filterDefinition.Exists(x => x.Timestamp, false),
                         filterDefinition.Eq(x => x.Timestamp, 0)) &
                     filterDefinition.Or(filterDefinition.Exists(x => x.Submitted, false),
                         filterDefinition.Eq(x => x.Submitted, false));
        return await Collection.Find(filter).ToListAsync();
    }

    public async Task UpdateSignedEffectiveBalance(List<SignEffectiveBalanceModel> batchSignEffectiveBalances)
    {
        _logger.LogInformation($"Updating {batchSignEffectiveBalances.Count} singed effective balances.");
        //use bulk write to update signature and batch number and timestamp
        //to reduce db load, page by 500 and update them
        const int pageSize = Pagination.DefaultDbPageSize;
        ;
        var totalPages = batchSignEffectiveBalances.Count / pageSize + 1;
        for (var pageNumber = 0; pageNumber < totalPages; pageNumber++)
        {
            _logger.LogInformation($"Updating page {pageNumber} of {totalPages}.");

            var batch = batchSignEffectiveBalances.Skip(pageNumber * pageSize).Take(pageSize).ToList();
            if (batch.Count == 0) break;

            var ops = batch.Select(x =>
            {
                var filter = Builders<SignEffectiveBalanceModel>.Filter.Eq(e => e.Account, x.Account) &
                             Builders<SignEffectiveBalanceModel>.Filter.Eq(e => e.EraIndex, x.EraIndex) &
                             Builders<SignEffectiveBalanceModel>.Filter.Eq(e => e.EffectiveBalance, x.EffectiveBalance);

                var update = Builders<SignEffectiveBalanceModel>.Update
                    .Set(s => s.Signature, x.Signature)
                    .Set(s => s.Timestamp, x.Timestamp)
                    .Set(s => s.BatchNumber, x.BatchNumber)
                    .Set(s => s.UpdatedAt, DateTime.UtcNow);
                return new UpdateOneModel<SignEffectiveBalanceModel>(filter, update);
            });

            await Collection.BulkWriteAsync(ops);
        }
    }

    public async Task<int> FindLatestEraIndex(string account)
    {
        //find the latest era index for the account
        var filter = filterDef.Eq(x => x.Account, account);
        var sort = Builders<SignEffectiveBalanceModel>.Sort.Descending(x => x.EraIndex);
        var result = await Collection.Find(filter).Sort(sort).FirstOrDefaultAsync();
        return result?.EraIndex ?? 0;
    }
}