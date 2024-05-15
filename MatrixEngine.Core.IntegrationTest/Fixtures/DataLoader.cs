using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.Events;
using MongoDB.Driver;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public interface IDataLoader
{
    Task LoadCase(string caseName);
    Task ClearCase();
    IMongoCollection<BalanceSnapshotModel> BalanceSnapshotCollection { get; }
    IMongoCollection<EraModel> EraCollection { get; }
    IMongoCollection<RewardCycleModel> RewardCycleCollection { get; }
    IMongoCollection<StakerModel> StakerCollection { get; }
    void Dispose();
}

public class DataLoader : IDataLoader
{
    private RewardCycleFixtures _rewardCycleFixtures;
    private EraFixtures _eraFixtures;
    private BalanceSnapshotFixture _balanceSnapshotFixtures;
    private StakerFixture _stakerFixtures;
    private IMongoCollection<BalanceSnapshotModel>? _balanceSnapshotCollection;
    private IMongoCollection<EraModel>? _eraCollection;
    private IMongoCollection<RewardCycleModel>? _rewardCycleCollection;
    private IMongoCollection<StakerModel>? _stakerCollection;
    private IMongoDatabase _database;
    private TransactionsFixture _transactionFixtures;
    private IMongoCollection<TransactionModel>? _transactionCollection;
    private EffectiveBalanceFixture _effectiveBalanceFixtures;
    private IMongoCollection<EffectiveBalanceModel>? _effectiveBalanceCollection;
    private BalanceChangeFixtures _balanceChangeFixtures;
    private IMongoCollection<BalanceChangeModel>? _balanceChangeCollection;

    public DataLoader(IMongoDatabase database)
    {
        _database = database;
        Initialise();
    }

    private void Initialise()
    {
        InitCollections();
        InitFixtures();
    }

    public async Task LoadCase(string caseName)
    {
        await ClearCase();
        await _eraFixtures.LoadData($"Data/{caseName}/eras.json", DbCollectionName.Era);
        await _transactionFixtures.LoadData($"Data/{caseName}/transactions.json", DbCollectionName.Transactions);
        await _balanceSnapshotFixtures.LoadData($"Data/{caseName}/balance-snapshots.json",
            DbCollectionName.BalanceSnapshot);
        await _rewardCycleFixtures.LoadData($"Data/{caseName}/reward-circle.json", DbCollectionName.RewardCycle);
        await _stakerFixtures.LoadData($"Data/{caseName}/stakers.json", DbCollectionName.Stakers);
    }

    private void InitFixtures()
    {
        _balanceSnapshotFixtures = new BalanceSnapshotFixture(_database);
        _eraFixtures = new EraFixtures(_database);
        _rewardCycleFixtures = new RewardCycleFixtures(_database);
        _stakerFixtures = new StakerFixture(_database);
        _transactionFixtures = new TransactionsFixture(_database);
        _effectiveBalanceFixtures = new EffectiveBalanceFixture(_database);
        _balanceChangeFixtures = new BalanceChangeFixtures(_database);
    }

    private void InitCollections()
    {
        _balanceSnapshotCollection = _database.GetCollection<BalanceSnapshotModel>(DbCollectionName.BalanceSnapshot);
        
        _balanceSnapshotCollection.Indexes.CreateOne(
            new CreateIndexModel<BalanceSnapshotModel>(
                Builders<BalanceSnapshotModel>.IndexKeys.Ascending(x => x.Account),
                new CreateIndexOptions {}
            )
        );
        
        _balanceSnapshotCollection.Indexes.CreateOne(
            new CreateIndexModel<BalanceSnapshotModel>(
                Builders<BalanceSnapshotModel>.IndexKeys.Ascending(x => x.EndBlock),
                new CreateIndexOptions {}
            )
        );
        
        _eraCollection = _database.GetCollection<EraModel>(DbCollectionName.Era);
        
        _eraCollection.Indexes.CreateOne(
            new CreateIndexModel<EraModel>(
                Builders<EraModel>.IndexKeys.Ascending(x => x.EraIndex),
                new CreateIndexOptions {}
            )
        );
        
        _rewardCycleCollection = _database.GetCollection<RewardCycleModel>(DbCollectionName.RewardCycle);
        _stakerCollection = _database.GetCollection<StakerModel>(DbCollectionName.Stakers);
        
        _stakerCollection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<StakerModel>(
                Builders<StakerModel>.IndexKeys.Ascending(x => x.EraIndex),
                new CreateIndexOptions {}
            ),
            new CreateIndexModel<StakerModel>(
                Builders<StakerModel>.IndexKeys.Ascending(x => x.Account),
                new CreateIndexOptions {}
            )
        });
        
        _transactionCollection = _database.GetCollection<TransactionModel>(DbCollectionName.Transactions);
        
        _transactionCollection.Indexes.CreateOne(
            new CreateIndexModel<TransactionModel>(
                Builders<TransactionModel>.IndexKeys.Ascending(x => x.BlockNumber),
                new CreateIndexOptions {}
            )
        );

        _effectiveBalanceCollection = _database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        
        _effectiveBalanceCollection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<EffectiveBalanceModel>(
                Builders<EffectiveBalanceModel>.IndexKeys.Ascending(x => x.StartBlock),
                new CreateIndexOptions {}
            ),
            new CreateIndexModel<EffectiveBalanceModel>(
                Builders<EffectiveBalanceModel>.IndexKeys.Ascending(x => x.EndBlock),
                new CreateIndexOptions {}
            ),
            new CreateIndexModel<EffectiveBalanceModel>(
                Builders<EffectiveBalanceModel>.IndexKeys.Ascending(x => x.Account),
                new CreateIndexOptions {}
            )
        });
        
        _balanceChangeCollection = _database.GetCollection<BalanceChangeModel>(DbCollectionName.Balance);
        
        _balanceChangeCollection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<BalanceChangeModel>(
                Builders<BalanceChangeModel>.IndexKeys.Ascending(x => x.Account),
                new CreateIndexOptions {}
            ),
            new CreateIndexModel<BalanceChangeModel>(
                Builders<BalanceChangeModel>.IndexKeys.Ascending(x => x.StartBlock),
                new CreateIndexOptions {}
            ),
            new CreateIndexModel<BalanceChangeModel>(
                Builders<BalanceChangeModel>.IndexKeys.Ascending(x => x.EndBlock),
                new CreateIndexOptions {}
            )
        });
    }

    public async Task ClearCase()
    {
        await _effectiveBalanceFixtures.ClearData(DbCollectionName.EffectiveBalance);
        await _balanceSnapshotFixtures.ClearData(DbCollectionName.BalanceSnapshot);
        await _eraFixtures.ClearData(DbCollectionName.Era);
        await _rewardCycleFixtures.ClearData(DbCollectionName.RewardCycle);
        await _stakerFixtures.ClearData(DbCollectionName.Stakers);
        await _transactionFixtures.ClearData(DbCollectionName.Transactions);
        await _balanceChangeFixtures.ClearData(DbCollectionName.Balance);
    }

    public IMongoCollection<EffectiveBalanceModel> EffectiveBalanceCollection
    {
        get
        {
            return _effectiveBalanceCollection ??=
                _database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        }
    }
    public IMongoCollection<BalanceSnapshotModel> BalanceSnapshotCollection
    {
        get
        {
            return _balanceSnapshotCollection ??=
                _database.GetCollection<BalanceSnapshotModel>(DbCollectionName.BalanceSnapshot);
        }
    }

    public IMongoCollection<EraModel> EraCollection
    {
        get { return _eraCollection ??= _database.GetCollection<EraModel>(DbCollectionName.Era); }
    }

    public IMongoCollection<RewardCycleModel> RewardCycleCollection
    {
        get { return _rewardCycleCollection ??= _database.GetCollection<RewardCycleModel>(DbCollectionName.RewardCycle); }
    }

    public IMongoCollection<StakerModel> StakerCollection
    {
        get { return _stakerCollection ??= _database.GetCollection<StakerModel>(DbCollectionName.Stakers); }
    }

    public IMongoCollection<TransactionModel> TransactionCollection
    {
        get { return _transactionCollection ??= _database.GetCollection<TransactionModel>(DbCollectionName.Transactions); }
    }
    
    public IMongoCollection<BalanceChangeModel> BalanceChangeCollection
    {
        get { return _balanceChangeCollection ??= _database.GetCollection<BalanceChangeModel>(DbCollectionName.Balance); }
    }

    public void Dispose()
    {
    }
}