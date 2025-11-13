using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public class BalanceSnapshotFixture: FixturesBase<BalanceSnapshotModel>
{
    public BalanceSnapshotFixture(IMongoDatabase database) : base(database)
    {
    }
}