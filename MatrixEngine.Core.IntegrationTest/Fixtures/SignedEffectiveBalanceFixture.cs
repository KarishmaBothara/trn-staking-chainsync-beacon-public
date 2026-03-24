using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MongoDB.Driver;
using System.Text.Json;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public class SignedEffectiveBalanceFixture
{
    private readonly IMongoDatabase _database;

    public SignedEffectiveBalanceFixture(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task LoadData(string filePath, string collectionName)
    {
        var collection = _database.GetCollection<SignedEffectiveBalanceModel>(collectionName);
        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<List<SignedEffectiveBalanceModel>>(json);

        if (data != null && data.Any())
        {
            await collection.InsertManyAsync(data);
        }
    }

    public async Task ClearData(string collectionName)
    {
        var collection = _database.GetCollection<SignedEffectiveBalanceModel>(collectionName);
        await collection.DeleteManyAsync(_ => true);
    }
} 