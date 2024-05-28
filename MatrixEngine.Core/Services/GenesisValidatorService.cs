using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface IGenesisValidatorService
{
    Task<List<GenesisValidatorModel>> GetGenesisValidators();
}

public class GenesisValidatorService : IGenesisValidatorService
{
    private readonly IMongoDatabase _database;

    public GenesisValidatorService(IMongoDatabase database)
    {
        _database = database;
    }

    private IMongoCollection<GenesisValidatorModel> Collection =>
        _database.GetCollection<GenesisValidatorModel>(DbCollectionName.GenesisValidators);

    
    public async Task<List<GenesisValidatorModel>> GetGenesisValidators()
    {
        return await Collection.Find(Builders<GenesisValidatorModel>.Filter.Empty).ToListAsync();
    }
}