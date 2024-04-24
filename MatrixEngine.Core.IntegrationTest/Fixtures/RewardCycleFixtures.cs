using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public class RewardCycleFixtures : FixturesBase<RewardCycleModel>
{
    public RewardCycleFixtures(IMongoDatabase database) : base(database)
    {
    }
}