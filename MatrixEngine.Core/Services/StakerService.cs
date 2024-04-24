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
    private ILogger<StakerService> _logger;

    public StakerService(IMongoDatabase database, ILogger<StakerService> logger)
    {
        _logger = logger;
        _database = database;
    }

    private IMongoCollection<StakerModel> Collection =>
        _database.GetCollection<StakerModel>(DbCollection.Stakers);

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

    public async Task<List<StakerModel>> GetAccountsStakerTypesByEraIndexes(List<string> accounts, List<int> eraIndexes)
    {
        var filter = Builders<StakerModel>.Filter.In(x => x.Account, accounts) &
                     Builders<StakerModel>.Filter.In(x => x.EraIndex, eraIndexes);
        var stakers = await Collection.Find(filter).ToListAsync();
        return stakers;
    }

    public async Task ResolveStakersAndSave(List<StakerNodeType> stakerTypes)
    {
        //create upsert ops and bulk write 
        var ops = stakerTypes.Select(stakerType =>
        {
            var filter = Builders<StakerModel>.Filter.Eq(x => x.Account, stakerType.Stash) &
                         Builders<StakerModel>.Filter.Eq(x => x.EraIndex, stakerType.EraIndex);
            var update = Builders<StakerModel>.Update
                .SetOnInsert(x => x.Account, stakerType.Stash)
                .SetOnInsert(x => x.EraIndex, stakerType.EraIndex)
                .Set(x => x.Type, stakerType.StakerType)
                .Set(x => x.TotalStake, stakerType.TotalStake)
                .Set(x => x.ValidatorStash, stakerType.ParentStash);

            return new UpdateOneModel<StakerModel>(filter, update) { IsUpsert = true };
        }).ToList();

        //bulk write
        await Collection.BulkWriteAsync(ops);
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
}