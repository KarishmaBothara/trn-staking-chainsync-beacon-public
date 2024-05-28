using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.Testing.Fixtures;

public class StakersFixture
{
    private readonly IMongoDatabase _database;

    public StakersFixture(IMongoDatabase database)
    {
        _database = database;
    }
    
    public void BuildData()
    {
        const string path = @"Data/stakers.json";
        var stakers = JsonFileReader.Read<List<StakerModel>>(path);

        var collection = _database.GetCollection<StakerModel>(DbCollectionName.Stakers);
        collection.InsertMany(stakers);
    }
    
    public void ClearData()
    {
        var collection = _database.GetCollection<StakerModel>(DbCollectionName.Stakers);
        collection.DeleteMany(Builders<StakerModel>.Filter.Empty);
    }
}
