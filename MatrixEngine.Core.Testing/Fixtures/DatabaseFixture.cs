using MatrixEngine.Core.Testing;
using Mongo2Go;
using MongoDB.Driver;

namespace MatrixEngine.Core.Testing.Fixtures;

public class DatabaseFixture : IDisposable
{
    internal static MongoDbRunner _runner;
    internal static string _databaseName = "IntegrationTest";
    internal static string _collectionName = "TestCollection";

    public DatabaseFixture()
    {
        Initialise();
    }

    private void Initialise()
    {
        _runner = MongoDbRunner.Start(singleNodeReplSet: true, singleNodeReplSetWaitTimeout: 10);

        var client = new MongoClient(_runner.ConnectionString);
        Database = client.GetDatabase(_databaseName);
    }

    public void BuildRewardCycleData()
    {
        var rewardCycleFixtures = new RewardCycleFixtures(Database);
        rewardCycleFixtures.BuildData();
    }

    public void ClearRewardCycleData()
    {
        var rewardCycleFixtures = new RewardCycleFixtures(Database);
        rewardCycleFixtures.ClearData();
    }

    public void BuildEraData()
    {
        var eraFixtures = new EraFixtures(Database);
        eraFixtures.BuildData();
    }

    public void ClearEraData()
    {
        var eraFixtures = new EraFixtures(Database);
        eraFixtures.ClearData();
    }

    public void BuildStakerData()
    {
        var stakersFixture = new StakersFixture(Database);
        stakersFixture.BuildData();
    }

    public void ClearStakerData()
    {
        var stakersFixture = new StakersFixture(Database);
        stakersFixture.ClearData();
    }

    public void BuildEffectiveBalanceData()
    {
        var effectiveBalanceFixture = new EffectiveBalanceFixture(Database);
        effectiveBalanceFixture.BuildData();
    }

    public void ClearEffectiveBalanceData()
    {
        var effectiveBalanceFixture = new EffectiveBalanceFixture(Database);
        effectiveBalanceFixture.ClearData();
    }

    public void BuildBalanceChangeData()
    {
        var balanceChangeFixture = new BalanceChangeFixture(Database);
        balanceChangeFixture.BuildData();
    }

    public void ClearBalanceChangeData()
    {
        var balanceChangeFixture = new BalanceChangeFixture(Database);
        balanceChangeFixture.ClearData();
    }

    public void BuildBalanceSnapshotData()
    {
        var balanceSnapshotFixture = new BalanceSnapshotFixture(Database);
        balanceSnapshotFixture.BuildData();
    }

    public void ClearBalanceSnapshotData()
    {
        var balanceSnapshotFixture = new BalanceSnapshotFixture(Database);
        balanceSnapshotFixture.ClearData();
    }

    public void BuildSignEffectiveBalanceData()
    {
        var signEffectiveBalanceFixture = new SignEffectiveBalanceFixture(Database);
        signEffectiveBalanceFixture.BuildData();
    }
    
    public void ClearSignEffectiveBalanceData()
    {
        var signEffectiveBalanceFixture = new SignEffectiveBalanceFixture(Database);
        signEffectiveBalanceFixture.ClearData();
    }
    
    public IMongoDatabase Database { set; get; }

    public void Dispose()
    {
        _runner.Dispose();
    }
}

[CollectionDefinition("Database Collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}