using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Exceptions;
using MatrixEngine.Core.GraphQL.ActiveEras;
using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IEraService
{
    Task<EraModel> GetEraByIndex(int eraIndex);
    Task<EraModel> GetLatestFinishedEra();
    Task<List<EraModel>> GetEraListByBlockRange(int startBlock, int endBlock);
    Task ResolveActiveErasAndSave(List<ActiveEraType> activeEraTypes);
}

public class EraService : IEraService
{
    private readonly IMongoDatabase _database;

    public EraService(IMongoDatabase database)
    {
        _database = database;
    }

    private IMongoCollection<EraModel> Collection => _database.GetCollection<EraModel>(DbCollectionName.Era);

    public async Task<EraModel> GetEraByIndex(int eraIndex)
    {
        var filter = Builders<EraModel>.Filter.Eq(x => x.EraIndex, eraIndex);

        var era = await Collection.Find(filter).FirstOrDefaultAsync();

        if (era == null) throw new EraException("Era not found");

        return era;
    }

    public async Task<EraModel> GetLatestFinishedEra()
    {
        var filter = Builders<EraModel>.Filter.Not(Builders<EraModel>.Filter.Eq(x => x.EndBlock, -1));
        var era = await Collection.Find(filter)
            .SortByDescending(x => x.EraIndex)
            .FirstOrDefaultAsync();

        if (era == null) throw new EraException("No finished era found");

        return era;
    }

    public async Task<List<EraModel>> GetEraListByBlockRange(int startBlock, int endBlock)
    {
        var filter = Builders<EraModel>.Filter.Gte(x => x.StartBlock, startBlock) &
                     Builders<EraModel>.Filter.Lte(x => x.EndBlock, endBlock);

        var eras = await Collection.Find(filter).ToListAsync();

        return eras;
    }

    public async Task ResolveActiveErasAndSave(List<ActiveEraType> activeEraTypes)
    {
        //as activeEraTypes only contain the block number that indicate the start block number for the era
        //we need to resolve the end block by checking the next era block number
        //minus one from next ear block number to get the end block number for the current era
        //then create new models with era start and end block numbers with era index  
        //at last upsert db
        var activeEraTypesCount = activeEraTypes.Count;

        var ops = new List<UpdateOneModel<EraModel>>();
        for (var i = 0; i < activeEraTypesCount; i++)
        {
            var activeEra = activeEraTypes[i];
            var eraIndex = activeEra.EraIndex;
            var startBlock = activeEra.BlockNumber;
            var endBlock = i + 1 < activeEraTypesCount ? activeEraTypes[i + 1].BlockNumber - 1 : -1;

            var filter = Builders<EraModel>.Filter.Eq(x => x.EraIndex, eraIndex);
            var update = Builders<EraModel>.Update
                .Set(x => x.EraIndex, eraIndex)
                .Set(x => x.StartBlock, startBlock)
                .Set(x => x.EndBlock, endBlock)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow);

            ops.Add(new UpdateOneModel<EraModel>(filter, update) { IsUpsert = true });
        }

        if (ops.Count > 0)
        {
            await Collection.BulkWriteAsync(ops);
        }
    }
}