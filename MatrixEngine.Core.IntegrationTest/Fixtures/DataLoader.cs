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
        await _eraFixtures.LoadData($"Data/{caseName}/eras.json", DbCollection.Era);
        await _transactionFixtures.LoadData($"Data/{caseName}/transactions.json", DbCollection.Transactions);
        await _balanceSnapshotFixtures.LoadData($"Data/{caseName}/balance-snapshots.json",
            DbCollection.BalanceSnapshot);
        await _rewardCycleFixtures.LoadData($"Data/{caseName}/reward-circle.json", DbCollection.RewardCycle);
        await _stakerFixtures.LoadData($"Data/{caseName}/stakers.json", DbCollection.Stakers);
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
        _balanceSnapshotCollection = _database.GetCollection<BalanceSnapshotModel>(DbCollection.BalanceSnapshot);
        
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
        
        _eraCollection = _database.GetCollection<EraModel>(DbCollection.Era);
        
        _eraCollection.Indexes.CreateOne(
            new CreateIndexModel<EraModel>(
                Builders<EraModel>.IndexKeys.Ascending(x => x.EraIndex),
                new CreateIndexOptions {}
            )
        );
        
        _rewardCycleCollection = _database.GetCollection<RewardCycleModel>(DbCollection.RewardCycle);
        _stakerCollection = _database.GetCollection<StakerModel>(DbCollection.Stakers);
        
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
        
        _transactionCollection = _database.GetCollection<TransactionModel>(DbCollection.Transactions);
        
        _transactionCollection.Indexes.CreateOne(
            new CreateIndexModel<TransactionModel>(
                Builders<TransactionModel>.IndexKeys.Ascending(x => x.BlockNumber),
                new CreateIndexOptions {}
            )
        );

        _effectiveBalanceCollection = _database.GetCollection<EffectiveBalanceModel>(DbCollection.EffectiveBalance);
        
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
        
        _balanceChangeCollection = _database.GetCollection<BalanceChangeModel>(DbCollection.Balance);
        
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
        await _effectiveBalanceFixtures.ClearData(DbCollection.EffectiveBalance);
        await _balanceSnapshotFixtures.ClearData(DbCollection.BalanceSnapshot);
        await _eraFixtures.ClearData(DbCollection.Era);
        await _rewardCycleFixtures.ClearData(DbCollection.RewardCycle);
        await _stakerFixtures.ClearData(DbCollection.Stakers);
        await _transactionFixtures.ClearData(DbCollection.Transactions);
        await _balanceChangeFixtures.ClearData(DbCollection.Balance);
    }

    public IMongoCollection<EffectiveBalanceModel> EffectiveBalanceCollection
    {
        get
        {
            return _effectiveBalanceCollection ??=
                _database.GetCollection<EffectiveBalanceModel>(DbCollection.EffectiveBalance);
        }
    }
    public IMongoCollection<BalanceSnapshotModel> BalanceSnapshotCollection
    {
        get
        {
            return _balanceSnapshotCollection ??=
                _database.GetCollection<BalanceSnapshotModel>(DbCollection.BalanceSnapshot);
        }
    }

    public IMongoCollection<EraModel> EraCollection
    {
        get { return _eraCollection ??= _database.GetCollection<EraModel>(DbCollection.Era); }
    }

    public IMongoCollection<RewardCycleModel> RewardCycleCollection
    {
        get { return _rewardCycleCollection ??= _database.GetCollection<RewardCycleModel>(DbCollection.RewardCycle); }
    }

    public IMongoCollection<StakerModel> StakerCollection
    {
        get { return _stakerCollection ??= _database.GetCollection<StakerModel>(DbCollection.Stakers); }
    }

    public IMongoCollection<TransactionModel> TransactionCollection
    {
        get { return _transactionCollection ??= _database.GetCollection<TransactionModel>(DbCollection.Transactions); }
    }
    
    public IMongoCollection<BalanceChangeModel> BalanceChangeCollection
    {
        get { return _balanceChangeCollection ??= _database.GetCollection<BalanceChangeModel>(DbCollection.Balance); }
    }

    public void Dispose()
    {
    }
}