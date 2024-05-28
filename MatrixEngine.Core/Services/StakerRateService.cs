using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IStakerRateService
{
    Task UpsertStakerRates(List<StakerRateModel> stakerRates);
    Task<List<StakerRateModel>> LoadLatestUnsignedStakerRates();
    Task UpdateSignatures(List<StakerRateModel> stakerRates);
}

public class StakerRateService : IStakerRateService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<StakerRateService> _logger;

    private readonly FilterDefinitionBuilder<StakerRateModel> _filterDef =
        Builders<StakerRateModel>.Filter;
    private IMongoCollection<StakerRateModel> Collection =>
        _database.GetCollection<StakerRateModel>(DbCollectionName.StakerRates);

    public StakerRateService(IMongoDatabase database, ILogger<StakerRateService> logger)
    {
        _logger = logger;
        _database = database;
        
        Collection.Indexes.CreateOne(new CreateIndexModel<StakerRateModel>(
            Builders<StakerRateModel>.IndexKeys.Ascending(x => x.Account)
                .Ascending(x => x.EraIndex)
        ));
    }

    public async Task<List<StakerRateModel>> LoadLatestUnsignedStakerRates()
    {
        var filter = _filterDef.Eq(x => x.BatchNumber, null) &
                     _filterDef.Eq(x => x.Signature, null) &
                     _filterDef.Or(_filterDef.Exists(x => x.Timestamp, false),
                         _filterDef.Eq(x => x.Timestamp, 0));
        return await Collection.Find(filter).ToListAsync();
    }

    public async Task UpsertStakerRates(List<StakerRateModel> stakerRates)
    {
        // bulk write to insert data
        _logger.LogInformation($"Upserting {stakerRates.Count} staker rates.");

        //to reduce db load, page by 500 and insert them
        const int pageSize = Pagination.DefaultDbPageSize;
        ;
        var totalPages = stakerRates.Count / pageSize + 1;

        for (var pageNumber = 0; pageNumber < totalPages; pageNumber++)
        {
            _logger.LogInformation($"Upserting page {pageNumber} of {totalPages}.");

            var batch = stakerRates.Skip(pageNumber * pageSize).Take(pageSize).ToList();
            if (batch.Count == 0) break;

            //bulk upsert the data
            var bulkOps = new List<UpdateOneModel<StakerRateModel>>();
            foreach (var rate in batch)
            {
                var filter = _filterDef.Eq(x => x.Account, rate.Account) &
                             _filterDef.Eq(x => x.EraIndex, rate.EraIndex) &
                             _filterDef.Eq(x => x.Type, rate.Type);

                var update = Builders<StakerRateModel>.Update
                    .SetOnInsert(x => x.CreatedAt, rate.CreatedAt)
                    .Set(x => x.UpdatedAt, rate.UpdatedAt)
                    .Set(x => x.Account, rate.Account)
                    .Set(x => x.Type, rate.Type)
                    .Set(x => x.Rate, rate.Rate);

                bulkOps.Add(new UpdateOneModel<StakerRateModel>(filter, update)
                {
                    IsUpsert = true
                });
            }

            await Collection.BulkWriteAsync(bulkOps);
        }
    }

    public async Task UpdateSignatures(List<StakerRateModel> stakerRates)
    {
        // bulk write to insert data
        _logger.LogInformation($"Updating {stakerRates.Count} staker rates signatures.");

        //to reduce db load, page by 500 and insert them
        const int pageSize = Pagination.DefaultDbPageSize;

        var totalPages = stakerRates.Count / pageSize + 1;

        for (var pageNumber = 0; pageNumber < totalPages; pageNumber++)
        {
            _logger.LogInformation($"Updating page {pageNumber} of {totalPages}.");

            var batch = stakerRates.Skip(pageNumber * pageSize).Take(pageSize).ToList();
            if (batch.Count == 0) break;

            //bulk upsert the data
            var bulkOps = new List<UpdateOneModel<StakerRateModel>>();
            foreach (var rate in batch)
            {
                var filter = _filterDef.Eq(x => x.Account, rate.Account) &
                             _filterDef.Eq(x => x.EraIndex, rate.EraIndex) &
                             _filterDef.Eq(x => x.Rate, rate.Rate);

                var update = Builders<StakerRateModel>.Update
                    .Set(x => x.Signature, rate.Signature)
                    .Set(x => x.Timestamp, rate.Timestamp)
                    .Set(x => x.BatchNumber, rate.BatchNumber);

                bulkOps.Add(new UpdateOneModel<StakerRateModel>(filter, update)
                {
                    IsUpsert = false
                });
            }

            await Collection.BulkWriteAsync(bulkOps);
        }
    }
}