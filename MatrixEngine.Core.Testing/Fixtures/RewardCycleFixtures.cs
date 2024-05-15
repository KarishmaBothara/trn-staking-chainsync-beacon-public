using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.Testing.Fixtures;

public class RewardCycleFixtures
{
    private readonly IMongoDatabase _database;

    public RewardCycleFixtures(IMongoDatabase database)
    {
        _database = database;
    }

    public void BuildData()
    {
        const string path = @"Data/reward-cycles.json";
        var rewardCycles = JsonFileReader.Read<List<RewardCycleModel>>(path);

        var collection = _database.GetCollection<RewardCycleModel>(DbCollectionName.RewardCycle);
        collection.InsertMany(rewardCycles);
    }
    
    public void ClearData()
    {
        var collection = _database.GetCollection<RewardCycleModel>(DbCollectionName.RewardCycle);
        collection.DeleteMany(Builders<RewardCycleModel>.Filter.Empty);
    }
}