using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.Testing.Fixtures;

public class BalanceSnapshotFixture
{
    private readonly IMongoDatabase _database;

    public BalanceSnapshotFixture(IMongoDatabase database)
    {
        _database = database;
    }

    public void BuildData()
    {
        const string path = @"Data/balance-snapshots.json";
        var balanceSnapshots = JsonFileReader.Read<List<BalanceSnapshotModel>>(path);

        var collection = _database.GetCollection<BalanceSnapshotModel>(DbCollectionName.BalanceSnapshot);
        collection.InsertMany(balanceSnapshots);
    }
    
    public void ClearData()
    {
        var collection = _database.GetCollection<BalanceSnapshotModel>(DbCollectionName.BalanceSnapshot);
        collection.DeleteMany(Builders<BalanceSnapshotModel>.Filter.Empty);
    }
}