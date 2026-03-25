using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface ISignEffectiveBalanceService
{
    Task InsertSignEffectiveBalance(List<SignedEffectiveBalanceModel> signEffectiveBalanceModels);
}

public class SignEffectiveBalanceService : ISignEffectiveBalanceService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<SignEffectiveBalanceService> _logger;
    private readonly FilterDefinitionBuilder<SignedEffectiveBalanceModel> _filterDef =
        Builders<SignedEffectiveBalanceModel>.Filter;
    private IMongoCollection<SignedEffectiveBalanceModel> Collection =>
        _database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);

    public SignEffectiveBalanceService(IMongoDatabase database, ILogger<SignEffectiveBalanceService> logger)
    {
        _logger = logger;
        _database = database;

        Collection.Indexes.CreateOne(new CreateIndexModel<SignedEffectiveBalanceModel>(
            Builders<SignedEffectiveBalanceModel>.IndexKeys.Ascending(x => x.Account)
                .Ascending(x => x.VtxDistributionId)
                .Ascending(x => x.TotalRewardPoints)
        ));
    }

    public async Task InsertSignEffectiveBalance(List<SignedEffectiveBalanceModel> signEffectiveBalanceModels)
    {
        // bulk write to insert data
        _logger.LogInformation($"Inserting {signEffectiveBalanceModels.Count} ready to sign effective balances.");

        //to reduce db load, page by 500 and insert them
        const int pageSize = Pagination.DefaultDbPageSize;

        var totalPages = signEffectiveBalanceModels.Count / pageSize + 1;

        for (var pageNumber = 0; pageNumber < totalPages; pageNumber++)
        {
            _logger.LogInformation($"Inserting page {pageNumber} of {totalPages}.");

            var batch = signEffectiveBalanceModels.Skip(pageNumber * pageSize).Take(pageSize).ToList();
            if (batch.Count == 0) break;
            var ops = batch.Select(x =>
            {
                var filter = _filterDef.Eq(f => f.Account, x.Account) &
                             _filterDef.Eq(f => f.StartBlock, x.StartBlock) &
                             _filterDef.Eq(f => f.EndBlock, x.EndBlock);

                var update = Builders<SignedEffectiveBalanceModel>.Update
                    .SetOnInsert(u => u.CreatedAt, DateTime.UtcNow)
                    .SetOnInsert(u => u.Account, x.Account)
                    .SetOnInsert(u => u.StartBlock, x.StartBlock)
                    .SetOnInsert(u => u.EndBlock, x.EndBlock)
                    .Set(u => u.TotalRewardPoints, x.TotalRewardPoints)
                    .Set(u => u.VtxDistributionId, x.VtxDistributionId)
//                     .Set(u => u.Signature, x.Signature)
                    .Set(u => u.Timestamp, x.Timestamp)
                    .Set(u => u.Verified, x.Verified)
                    .Set(u => u.Submitted, x.Submitted)
                    .Set(u => u.UpdatedAt, DateTime.UtcNow);
                return new UpdateOneModel<SignedEffectiveBalanceModel>(filter, update) { IsUpsert = true };
            });

            await Collection.BulkWriteAsync(ops);
        }
    }

}
