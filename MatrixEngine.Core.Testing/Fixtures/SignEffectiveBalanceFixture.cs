using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.Testing.Fixtures;

public class SignEffectiveBalanceFixture
{
    private readonly IMongoDatabase _database;

    public SignEffectiveBalanceFixture(IMongoDatabase database)
    {
        _database = database;
    }

    public void BuildData()
    {
        const string path = @"Data/sign-effective-balances.json";
        var data = JsonFileReader.Read<List<SignEffectiveBalanceModel>>(path);

        var collection = _database.GetCollection<SignEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        collection.InsertMany(data);
    }

    public void ClearData()
    {
        var collection = _database.GetCollection<SignEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        collection.DeleteMany(Builders<SignEffectiveBalanceModel>.Filter.Empty);
    }
}