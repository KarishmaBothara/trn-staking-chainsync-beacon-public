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

public class TransactionEventService : ITransactionEventService
{
    private readonly IMongoDatabase _database;
    private ILogger<TransactionEventService> _logger;

    public TransactionEventService(IMongoDatabase database, ILogger<TransactionEventService> logger)
    {
        _logger = logger;
        _database = database;
    }

    private IMongoCollection<TransactionModel> Collection =>
        _database.GetCollection<TransactionModel>(DbCollectionName.Transactions);


    public Task<List<TransactionModel>> GetTransactionEventsByBlockRange(int startBlock, int endBlock)
    {
        var filter = Builders<TransactionModel>.Filter.Gte(x => x.BlockNumber, startBlock) &
                     Builders<TransactionModel>.Filter.Lte(x => x.BlockNumber, endBlock);

        return Collection.Find(filter).ToListAsync();
    }

    public async Task UpsertTransactionEvents(List<TransactionModel> transactionEvents)
    {
        if (!transactionEvents.Any())
        {
            _logger.LogInformation("No transaction events to upsert");
            return;
        }

        _logger.LogInformation($"Upserting {transactionEvents.Count} transaction events");

        var bulkOps = new List<UpdateOneModel<TransactionModel>>();
        foreach (var transaction in transactionEvents)
        {
            var filter = Builders<TransactionModel>.Filter.Eq(x => x.Account, transaction.Account) &
                         Builders<TransactionModel>.Filter.Eq(x => x.Amount, transaction.Amount) &
                         Builders<TransactionModel>.Filter.Eq(x => x.BlockNumber, transaction.BlockNumber);
            var update = Builders<TransactionModel>.Update.Set(x => x.Type, transaction.Type)
                .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);
            
            bulkOps.Add(new UpdateOneModel<TransactionModel>(filter, update) { IsUpsert = true });
        }

        await Collection.BulkWriteAsync(bulkOps);
    }

    // Get the latest block number from the transaction events, this is to prevent fetching indexed results multiple times per block
    public async Task<int> GetLatestBlockNumber()
    {
        var sort = Builders<TransactionModel>.Sort.Descending(x => x.BlockNumber);
        var latestBlock = await Collection.Find(x => true).Sort(sort).FirstOrDefaultAsync();
        return latestBlock?.BlockNumber ?? 0;
    }
}