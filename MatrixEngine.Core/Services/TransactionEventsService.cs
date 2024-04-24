using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models.Events;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface ITransactionEventService
{
    Task<List<TransactionModel>> GetTransactionEventsByBlockRange(int startBlock, int endBlock);
    Task UpsertTransactionEvents(List<TransactionModel> transactionEvents);
    Task<int> GetLatestBlockNumber();
}
public class TransactionEventService: ITransactionEventService
{    
    private readonly IMongoDatabase _database;
    private ILogger<TransactionEventService> _logger;
    public TransactionEventService(IMongoDatabase database, ILogger<TransactionEventService> logger)
    {
        _logger = logger;
        _database = database;
    }
    private IMongoCollection<TransactionModel> Collection =>
        _database.GetCollection<TransactionModel>(DbCollection.Transactions);


    public Task<List<TransactionModel>> GetTransactionEventsByBlockRange(int startBlock, int endBlock)
    {
        var filter = Builders<TransactionModel>.Filter.Gte(x => x.BlockNumber, startBlock) &
                     Builders<TransactionModel>.Filter.Lte(x => x.BlockNumber, endBlock);
        
        return Collection.Find(filter).ToListAsync();
    }

    public async Task UpsertTransactionEvents(List<TransactionModel> transactionEvents)
    {
        var ops = transactionEvents.Select(t =>
        {
            //filter by account, amount, blockNumber
            var filter = Builders<TransactionModel>.Filter.Eq(x => x.Account, t.Account) &
                         Builders<TransactionModel>.Filter.Eq(x => x.Amount, t.Amount) &
                         Builders<TransactionModel>.Filter.Eq(x => x.BlockNumber, t.BlockNumber);
            var update = Builders<TransactionModel>.Update.Set(x => x.Type, t.Type);
            return new UpdateOneModel<TransactionModel>(filter, update) { IsUpsert = true };
        });
        
        await Collection.BulkWriteAsync(ops);
    }
    
    public async Task<int> GetLatestBlockNumber()
    {
        var sort = Builders<TransactionModel>.Sort.Descending(x => x.BlockNumber);
        var latestBlock = await Collection.Find(x => true).Sort(sort).FirstOrDefaultAsync();
        return latestBlock?.BlockNumber ?? 0;
    }
}