using MatrixEngine.Core.Constants;
using MatrixEngine.Core.GraphQL.Stakers;
using MatrixEngine.Core.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IStakerService
{
    Task<string?> GetAccountType(string account, int eraIndex);
    Task<List<StakerModel>> GetAccountStakerTypesByEraIndexes(string account, List<int> eraIndexes);
    Task<List<StakerModel>> GetAllStakerTypesByEraIndex(int eraIndex);
    Task<List<StakerModel>> GetAccountsStakerTypesByEraIndexes(List<string> accounts, List<int> eraIndexes);
    Task ResolveStakersAndSave(List<StakerNodeType> stakerTypes);

    /// <summary>
    /// Get the latest era fetched staker types
    /// </summary>
    /// <returns></returns>
    Task<int> GetLatestEraFetchedStakerTypes();
}

public class StakerService : IStakerService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<StakerService> _logger;

    public StakerService(IMongoDatabase database, ILogger<StakerService> logger)
    {
        _logger = logger;
        _database = database;
        EnsureStakerIndexes();
    }

    private IMongoCollection<StakerModel> Collection =>
        _database.GetCollection<StakerModel>(DbCollectionName.Stakers);

    public async Task<string?> GetAccountType(string account, int eraIndex)
    {
        var filter = Builders<StakerModel>.Filter.Eq(x => x.Account, account) &
                     Builders<StakerModel>.Filter.Eq(x => x.EraIndex, eraIndex);
        var stakers = await Collection.Find(filter).SortByDescending(x => x.Type).ToListAsync();

        var staker = stakers.Find(x => x.Type == StakerType.Validator || x.Type == StakerType.Nominator);

        return staker != null ? staker.Type : StakerType.Staker;
    }

    public async Task<List<StakerModel>> GetAccountStakerTypesByEraIndexes(string account, List<int> eraIndexes)
    {
        var filter = Builders<StakerModel>.Filter.Eq(x => x.Account, account) &
                     Builders<StakerModel>.Filter.In(x => x.EraIndex, eraIndexes);
        var stakers = await Collection.Find(filter).ToListAsync();
        return stakers;
    }

    public async Task<List<StakerModel>> GetAllStakerTypesByEraIndex(int eraIndex)
    {
        var filter = Builders<StakerModel>.Filter.Eq(x => x.EraIndex, eraIndex);
        var stakers = await Collection.Find(filter).ToListAsync();
        return stakers;
    }

    public async Task<List<StakerModel>> GetAccountsStakerTypesByEraIndexes(List<string> accounts, List<int> eraIndexes)
    {
        var filter = Builders<StakerModel>.Filter.In(x => x.Account, accounts) &
                     Builders<StakerModel>.Filter.In(x => x.EraIndex, eraIndexes);
        var stakers = await Collection.Find(filter).ToListAsync();
        return stakers;
    }

    public async Task ResolveStakersAndSave(List<StakerNodeType> stakerTypes)
    {
        //to reduce db load, page by 500 and insert them
        var pageSize = Pagination.DefaultDbPageSize;
        var totalPages = stakerTypes.Count / pageSize + 1;

        for (var pageNumber = 0; pageNumber < totalPages; pageNumber++)
        {
            var batch = stakerTypes.Skip(pageNumber * pageSize).Take(pageSize).ToList();
            //create upsert ops and bulk write 
            var ops = batch.Select(stakerType =>
            {
                var filter = Builders<StakerModel>.Filter.Eq(x => x.Account, stakerType.Stash) &
                             Builders<StakerModel>.Filter.Eq(x => x.EraIndex, stakerType.EraIndex);
                var update = Builders<StakerModel>.Update
                    .SetOnInsert(x => x.Account, stakerType.Stash)
                    .SetOnInsert(x => x.EraIndex, stakerType.EraIndex)
                    .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow)
                    .Set(x => x.Type, stakerType.StakerType)
                    .Set(x => x.TotalStake, stakerType.TotalStake)
                    .Set(x => x.ValidatorStash, stakerType.ParentStash);

                return new UpdateOneModel<StakerModel>(filter, update) { IsUpsert = true };
            }).ToList();

            if (ops.Count == 0)
            {
                _logger.LogWarning($"Ops length is 0, skipping");
                continue;
            }
            _logger.LogInformation($"Inserting {ops.Count} stakers to db");
            //bulk write
            var result = await Collection.BulkWriteAsync(ops);
            _logger.LogInformation($"Bulk write result: Inserted {result.InsertedCount}, Modified {result.ModifiedCount}, Matched {result.MatchedCount}");
            _logger.LogInformation($"Finished inserting {ops.Count} stakers to db");
        }
    }

    /// <summary>
    /// Get the latest era fetched staker types
    /// </summary>
    /// <returns></returns>
    public async Task<int> GetLatestEraFetchedStakerTypes()
    {
        //get the largest eraIndex in stakers collection
        //descending order by eraIndex and get the first one
        var staker = await Collection.Find(Builders<StakerModel>.Filter.Empty)
            .SortByDescending(x => x.EraIndex)
            .FirstOrDefaultAsync();

        _logger.LogInformation($"Latest era fetched staker types: {staker?.EraIndex}");


        if (staker != null) return staker.EraIndex;

        return -1;
    }

    /// Creates necessary indexes for the Stakers collection
    public void EnsureStakerIndexes()
    {
        try
        {
            _logger.LogInformation("Creating indexes for stakers collection");
            
            // Try-catch each index creation separately to handle cases where some indexes already exist
            try
            {
                // Create a compound index on Account + EraIndex
                // This will greatly speed up your queries that filter on these fields
                var compoundIndexModel = new CreateIndexModel<StakerModel>(
                    Builders<StakerModel>.IndexKeys
                        .Ascending(s => s.Account)
                        .Ascending(s => s.EraIndex),
                    new CreateIndexOptions 
                    { 
                        Name = "account_eraIndex_compound",
                        Background = true,   // Create in background to avoid blocking operations
                        Unique = true        // Enforce uniqueness at the database level
                    }
                );
                
                Collection.Indexes.CreateOne(compoundIndexModel);
                _logger.LogInformation("Successfully created compound index on Account and EraIndex");
            }
            catch (MongoCommandException ex) when (ex.Message.Contains("Index already exists"))
            {
                _logger.LogInformation("Compound index on Account and EraIndex already exists");
            }
            
            try
            {
                // Create a single-field index on EraIndex to support queries that filter only by era
                var eraIndexModel = new CreateIndexModel<StakerModel>(
                    Builders<StakerModel>.IndexKeys.Ascending(s => s.EraIndex),
                    new CreateIndexOptions 
                    { 
                        Name = "eraIndex",
                        Background = true
                    }
                );
                
                Collection.Indexes.CreateOne(eraIndexModel);
                _logger.LogInformation("Successfully created index on EraIndex");
            }
            catch (MongoCommandException ex) when (ex.Message.Contains("Index already exists"))
            {
                _logger.LogInformation("Index on EraIndex already exists");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating indexes for stakers collection");
            throw;
        }
    }
}