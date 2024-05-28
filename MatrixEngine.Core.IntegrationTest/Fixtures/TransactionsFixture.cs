using MatrixEngine.Core.Models.Events;
using MongoDB.Driver;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public class TransactionsFixture: FixturesBase<TransactionModel>
{
    public TransactionsFixture(IMongoDatabase database) : base(database)
    {
    }
}