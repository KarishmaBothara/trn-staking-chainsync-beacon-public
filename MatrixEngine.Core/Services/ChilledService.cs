using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IChilledService
{
    Task<List<ChilledModel>> GetChilledEventsByBlockRange(int startBlock, int endBlock);
    Task UpsertChilledEvents(List<ChilledModel> chilledEvents);
}

public class ChilledService : IChilledService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<ChilledService> _logger;
    private readonly FilterDefinitionBuilder<ChilledModel> _filterDef = Builders<ChilledModel>.Filter;
    private IMongoCollection<ChilledModel> Collection =>
        _database.GetCollection<ChilledModel>(DbCollectionName.Chilled);

    public ChilledService(IMongoDatabase database, ILogger<ChilledService> logger)
    {
        _logger = logger;
        _database = database;
    }

    public Task<List<ChilledModel>> GetChilledEventsByBlockRange(int startBlock, int endBlock)
    {
        var filter = Builders<ChilledModel>.Filter.Gte(x => x.BlockNumber, startBlock) &
                     Builders<ChilledModel>.Filter.Lte(x => x.BlockNumber, endBlock);

        return Collection.Find(filter).ToListAsync();
    }

    public async Task UpsertChilledEvents(List<ChilledModel> chilledEvents)
    {
        _logger.LogInformation($"Upserting {chilledEvents.Count} chilled events");
        
        var pageSize = Pagination.DefaultDbPageSize;
        var totalPages = chilledEvents.Count / pageSize + 1;

        for (var pageNumber = 0; pageNumber < totalPages; pageNumber++)
        {
            _logger.LogInformation($"Bulk Upserting page {pageNumber} of {totalPages}.");
            var batch = chilledEvents.Skip(pageNumber * pageSize).Take(pageSize).ToList();
            if(batch.Count == 0) break;

            var bulkOps = new List<WriteModel<ChilledModel>>();
            foreach (var item in batch)
            {
                var filter = _filterDef.And(
                    _filterDef.Eq(x => x.Account, item.Account),
                    _filterDef.Eq(x => x.BlockNumber, item.BlockNumber)
                );

                var update = Builders<ChilledModel>.Update
                    .SetOnInsert(x => x.Id, item.Id)
                    .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow)
                    .SetOnInsert(x => x.Account, item.Account)
                    .SetOnInsert(x => x.BlockNumber, item.BlockNumber)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow);

                var upsertOne = new UpdateOneModel<ChilledModel>(filter, update) { IsUpsert = true };
                bulkOps.Add(upsertOne);
            }

            await Collection.BulkWriteAsync(bulkOps);
        }
    }
} 