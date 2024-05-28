using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.Testing.Fixtures;

public class BalanceChangeFixture
{
    private readonly IMongoDatabase _database;

    public BalanceChangeFixture(IMongoDatabase database)
    {
        _database = database;
    }
    
    public void BuildData()
    {
        const string path = @"Data/balance-changes.json";
        var data = JsonFileReader.Read<List<BalanceModel>>(path);

        var collection = _database.GetCollection<BalanceModel>(DbCollectionName.Balance);
        collection.InsertMany(data);
    }
    
    public void ClearData()
    {
        var collection = _database.GetCollection<BalanceModel>(DbCollectionName.Balance);
        collection.DeleteMany(Builders<BalanceModel>.Filter.Empty);
    }
}