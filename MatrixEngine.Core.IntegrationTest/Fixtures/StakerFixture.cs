using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public class StakerFixture: FixturesBase<StakerModel>
{
    public StakerFixture(IMongoDatabase database) : base(database)
    {
    }
}