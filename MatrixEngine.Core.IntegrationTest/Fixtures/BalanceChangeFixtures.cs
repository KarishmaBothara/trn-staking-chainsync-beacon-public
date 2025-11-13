using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public class BalanceChangeFixtures : FixturesBase<BalanceChangeModel>
{
    public BalanceChangeFixtures(IMongoDatabase database) : base(database)
    {
    }
}
    