using MongoDB.Driver;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public class FixturesBase<T>
{
    private readonly IMongoDatabase _database;

    protected FixturesBase(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task LoadData(string path, string collectionName)
    {
        var list = JsonFileReader.Read<List<T>>(path);

        var collection = _database.GetCollection<T>(collectionName);
        await collection.InsertManyAsync(list);
    }

    public async Task ClearData(string collectionName)
    {
        var collection = _database.GetCollection<T>(collectionName);
        await collection.DeleteManyAsync(Builders<T>.Filter.Empty);
    }
}