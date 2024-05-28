using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IAccountPunishmentMarkService
{
    Task UpsertAccountPunishmentMarks(List<AccountPunishmentMarkModel> models);

    /// <summary>
    /// Load account punishment marks by block range
    /// It loads data with the conditions that if it is computed and in block ranges
    /// </summary>
    /// <param name="rewardCycleStartBlock"></param>
    /// <param name="rewardCycleEndBlock"></param>
    /// <returns></returns>
    Task<List<AccountPunishmentMarkModel>> LoadNewPunishmentMarksByBlockRange(int rewardCycleStartBlock,
        int rewardCycleEndBlock);

    Task UpdatePunishmentMarksApplied(List<AccountPunishmentMarkModel> punishmentMarks);
}

public class AccountPunishmentMarkService : IAccountPunishmentMarkService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<AccountPunishmentMarkService> _logger;

    private IMongoCollection<AccountPunishmentMarkModel> Collection =>
        _database.GetCollection<AccountPunishmentMarkModel>(DbCollectionName.AccountPunishmentMark);

    public AccountPunishmentMarkService(IMongoDatabase database, ILogger<AccountPunishmentMarkService> logger)
    {
        _logger = logger;
        _database = database;
    }

    public async Task UpsertAccountPunishmentMarks(List<AccountPunishmentMarkModel> models)
    {
        _logger.LogInformation($"Upserting {models.Count} account punishment marks");

        //to reduce db load, page by 500 and insert them
        var pageSize = 500;
        var totalPages = models.Count / pageSize + 1;

        for (var pageNumber = 0; pageNumber < totalPages; pageNumber++)
        {
            _logger.LogInformation($"Bulk Upserting page {pageNumber} of {totalPages}.");
            var batch = models.Skip(pageNumber * pageSize).Take(pageSize).ToList();
            if(batch.Count == 0) break;
            
            var ops = batch.Select(model =>
            {
                var filter = Builders<AccountPunishmentMarkModel>.Filter.Eq(m => m.Account, model.Account) &
                             Builders<AccountPunishmentMarkModel>.Filter.Eq(m => m.BlockNumber, model.BlockNumber) &
                             Builders<AccountPunishmentMarkModel>.Filter.Eq(m => m.Amount, model.Amount);
                var updateOne = Builders<AccountPunishmentMarkModel>
                    .Update
                    .SetOnInsert(x => x.Account, model.Account)
                    .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow)
                    .Set(m => m.Amount, model.Amount)
                    .Set(m => m.BlockNumber, model.BlockNumber);

                return new UpdateOneModel<AccountPunishmentMarkModel>(filter, updateOne) { IsUpsert = true };
            }).ToList();

            await Collection.BulkWriteAsync(ops);
        }
    }

    public async Task<List<AccountPunishmentMarkModel>> LoadNewPunishmentMarksByBlockRange(int rewardCycleStartBlock,
        int rewardCycleEndBlock)
    {
        var filterDef = Builders<AccountPunishmentMarkModel>.Filter;
        var filter = filterDef.Or(filterDef.Eq(x => x.Applied, false),
                         filterDef.Exists(x => x.Applied, false)) &
                     filterDef.And(
                         filterDef.Gte(x => x.BlockNumber, rewardCycleStartBlock),
                         filterDef.Lte(x => x.BlockNumber, rewardCycleEndBlock)
                     );


        return await Collection.Find(filter).ToListAsync();
    }

    public async Task UpdatePunishmentMarksApplied(List<AccountPunishmentMarkModel> punishmentMarks)
    {
        //to reduce db load, page by 500 and insert them
        var pageSize = 500;
        var totalPages = punishmentMarks.Count / pageSize + 1;
        
        for (var pageNumber = 0; pageNumber < totalPages; pageNumber++)
        {
            _logger.LogInformation($"Updating page {pageNumber} of {totalPages}.");

            var batch = punishmentMarks.Skip(pageNumber * pageSize).Take(pageSize).ToList();
            if(batch.Count == 0) break;
            
            var ops = batch.Select(x =>
            {
                var filter = Builders<AccountPunishmentMarkModel>.Filter.Eq(x => x.Account, x.Account) &
                             Builders<AccountPunishmentMarkModel>.Filter.Eq(x => x.BlockNumber, x.BlockNumber) &
                             Builders<AccountPunishmentMarkModel>.Filter.Eq(x => x.Amount, x.Amount);

                var update = Builders<AccountPunishmentMarkModel>.Update.Set(x => x.Applied, true);
                return new UpdateOneModel<AccountPunishmentMarkModel>(filter, update);
            });

            await Collection.BulkWriteAsync(ops);
        }
    }
}