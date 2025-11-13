using MatrixEngine.Core.Models;
using MongoDB.Driver;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public class EraFixtures : FixturesBase<EraModel>
{
    public EraFixtures(IMongoDatabase database) : base(database)
    {
    }
}