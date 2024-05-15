using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.Testing.Fixtures;

public class EraFixtures
{
    private readonly IMongoDatabase _database;

    public EraFixtures(IMongoDatabase database)
    {
        _database = database;
    }

    public void BuildData()
    {
        const string path = @"Data/eras.json";
        var eras = JsonFileReader.Read<List<EraModel>>(path);

        var collection = _database.GetCollection<EraModel>(DbCollectionName.Era);
        collection.InsertMany(eras);
    }
    
    public void ClearData()
    {
        var collection = _database.GetCollection<EraModel>(DbCollectionName.Era);
        collection.DeleteMany(Builders<EraModel>.Filter.Empty);
    }
}