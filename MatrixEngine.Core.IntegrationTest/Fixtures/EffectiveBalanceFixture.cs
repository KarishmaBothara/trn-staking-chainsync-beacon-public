using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public class EffectiveBalanceFixture : FixturesBase<EffectiveBalanceModel>
{
    public EffectiveBalanceFixture(IMongoDatabase database) : base(database)
    {
    }
}