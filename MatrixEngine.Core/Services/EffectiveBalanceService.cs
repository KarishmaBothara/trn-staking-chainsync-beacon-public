using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IEffectiveBalanceService
{
    Task<List<EffectiveBalanceModel>> GetEffectiveBalancesByAccount(string account);
    Task UpsertEffectiveBalance(List<EffectiveBalanceModel> data);
}

public class EffectiveBalanceService : IEffectiveBalanceService
{
    private readonly IMongoDatabase _database;

    public EffectiveBalanceService(IMongoDatabase database)
    {
        _database = database;
    }

    private IMongoCollection<EffectiveBalanceModel> Collection => _database.GetCollection<EffectiveBalanceModel>(DbCollection.EffectiveBalance);

    public async Task<List<EffectiveBalanceModel>> GetEffectiveBalancesByAccount(string account)
    {
        var filter = Builders<EffectiveBalanceModel>.Filter.Eq(x => x.Account, account);

        var effectiveBalances = await Collection.Find(filter).ToListAsync();

        return effectiveBalances;
    }

    public async Task UpsertEffectiveBalance(List<EffectiveBalanceModel> data)
    {
        //bulk upsert data
        var bulkOps = new List<WriteModel<EffectiveBalanceModel>>();
        foreach (var item in data)
        {
            var filter = Builders<EffectiveBalanceModel>.Filter.Eq(x => x.Account, item.Account) &
                         Builders<EffectiveBalanceModel>.Filter.Eq(x => x.StartBlock, item.StartBlock) & 
                         Builders<EffectiveBalanceModel>.Filter.Eq(x => x.EndBlock, item.EndBlock) ;

            var update = Builders<EffectiveBalanceModel>.Update
                .SetOnInsert(x => x.Account, item.Account)
                .SetOnInsert(x => x.EraIndex, item.EraIndex)
                .Set(x => x.StartBlock, item.StartBlock)
                .Set(x => x.EndBlock, item.EndBlock)
                .Set(x => x.Balance, item.Balance)
                .Set(x => x.EffectiveBalance, item.EffectiveBalance)
                .Set(x => x.EffectiveBlocks, item.EffectiveBlocks)
                .Set(x => x.Percentage, item.Percentage)
                .Set(x => x.Rate, item.Rate)
                .Set(x => x.Reward, item.Reward)
                .Set(x => x.Type, item.Type)
                .Set(x => x.EffectiveEras, item.EffectiveEras)
                .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow);

            var upsertOne = new UpdateOneModel<EffectiveBalanceModel>(filter, update) { IsUpsert = true };
            bulkOps.Add(upsertOne);
        }
        
        await Collection.BulkWriteAsync(bulkOps);
    }
    
}