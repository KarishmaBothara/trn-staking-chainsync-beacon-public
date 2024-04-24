using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.Testing.Fixtures;

public class EffectiveBalanceFixture
{
    private readonly IMongoDatabase _database;

    public EffectiveBalanceFixture(IMongoDatabase database)
    {
        _database = database;
    }

    public void BuildData()
    {
        const string path = @"Data/effective-balances.json";
        var data = JsonFileReader.Read<List<EffectiveBalanceModel>>(path);

        var collection = _database.GetCollection<EffectiveBalanceModel>(DbCollection.EffectiveBalance);
        collection.InsertMany(data);
    }

    public void ClearData()
    {
        var collection = _database.GetCollection<EffectiveBalanceModel>(DbCollection.EffectiveBalance);
        collection.DeleteMany(Builders<EffectiveBalanceModel>.Filter.Empty);
    }
}