using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public class ChilledFixtures : FixturesBase<ChilledModel>
{
    public ChilledFixtures(IMongoDatabase database) : base(database)
    {
    }
}