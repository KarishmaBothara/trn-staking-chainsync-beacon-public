using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.Events;
using MongoDB.Driver;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public interface IDataLoader
{
    Task LoadCase(string caseName);
    Task ClearCase();
    IMongoCollection<EraModel> EraCollection { get; }
    IMongoCollection<RewardCycleModel> RewardCycleCollection { get; }
    IMongoCollection<StakerModel> StakerCollection { get; }
    IMongoCollection<TransactionModel> TransactionCollection { get; }
    IMongoCollection<EffectiveBalanceModel> EffectiveBalanceCollection { get; }
    IMongoCollection<SignedEffectiveBalanceModel> SignedEffectiveBalanceCollection { get; }
    IMongoCollection<BalanceChangeModel> BalanceChangeCollection { get; }
    IMongoCollection<ChilledModel> ChilledCollection { get; }
    void Dispose();
}

public class DataLoader : IDataLoader
{
    private RewardCycleFixtures _rewardCycleFixtures;
    private EraFixtures _eraFixtures;
    private StakerFixture _stakerFixtures;
    private ChilledFixtures _chilledFixtures;
    private TransactionsFixture _transactionFixtures;
    private EffectiveBalanceFixture _effectiveBalanceFixtures;
    private SignedEffectiveBalanceFixture _signedEffectiveBalanceFixtures;
    private BalanceChangeFixtures _balanceChangeFixtures;
    
    private IMongoCollection<EraModel>? _eraCollection;
    private IMongoCollection<RewardCycleModel>? _rewardCycleCollection;
    private IMongoCollection<StakerModel>? _stakerCollection;
    private IMongoCollection<ChilledModel>? _chilledCollection;
    private IMongoCollection<TransactionModel>? _transactionCollection;
    private IMongoCollection<EffectiveBalanceModel>? _effectiveBalanceCollection;
    private IMongoCollection<BalanceChangeModel>? _balanceChangeCollection;
    private IMongoCollection<SignedEffectiveBalanceModel>? _signedEffectiveBalanceCollection;
    private IMongoDatabase _database;

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
        await _rewardCycleFixtures.LoadData($"Data/{caseName}/reward-cycle.json", DbCollectionName.RewardCycle);
        await _stakerFixtures.LoadData($"Data/{caseName}/stakers.json", DbCollectionName.Stakers);
        await _chilledFixtures.LoadData($"Data/{caseName}/chilled.json", DbCollectionName.Chilled);
    }

    private void InitFixtures()
    {
        _eraFixtures = new EraFixtures(_database);
        _rewardCycleFixtures = new RewardCycleFixtures(_database);
        _stakerFixtures = new StakerFixture(_database);
        _transactionFixtures = new TransactionsFixture(_database);
        _effectiveBalanceFixtures = new EffectiveBalanceFixture(_database);
        _balanceChangeFixtures = new BalanceChangeFixtures(_database);
        _chilledFixtures = new ChilledFixtures(_database);
        _signedEffectiveBalanceFixtures = new SignedEffectiveBalanceFixture(_database);
    }

    private void InitCollections()
    {
        _rewardCycleCollection = _database.GetCollection<RewardCycleModel>(DbCollectionName.RewardCycle);
        _rewardCycleCollection.Indexes.CreateOne(
            new CreateIndexModel<RewardCycleModel>(
                Builders<RewardCycleModel>.IndexKeys.Ascending(x => x.VtxDistributionId),
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
        
        _stakerCollection = _database.GetCollection<StakerModel>(DbCollectionName.Stakers);
        
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
        
        _chilledCollection = _database.GetCollection<ChilledModel>(DbCollectionName.Chilled);
        _chilledCollection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<ChilledModel>(
                Builders<ChilledModel>.IndexKeys.Ascending(x => x.Account),
                new CreateIndexOptions {}
            ),
            new CreateIndexModel<ChilledModel>(
                Builders<ChilledModel>.IndexKeys.Ascending(x => x.BlockNumber),
                new CreateIndexOptions {}
            )
        });

        _signedEffectiveBalanceCollection = _database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        _signedEffectiveBalanceCollection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<SignedEffectiveBalanceModel>(
                Builders<SignedEffectiveBalanceModel>.IndexKeys.Ascending(x => x.Account),
                new CreateIndexOptions {}
            ),
            new CreateIndexModel<SignedEffectiveBalanceModel>(
                Builders<SignedEffectiveBalanceModel>.IndexKeys.Ascending(x => x.VtxDistributionId),
                new CreateIndexOptions {}
            )
        });
    }

    public async Task ClearCase()
    {
        await _effectiveBalanceFixtures.ClearData(DbCollectionName.EffectiveBalance);
        await _eraFixtures.ClearData(DbCollectionName.Era);
        await _rewardCycleFixtures.ClearData(DbCollectionName.RewardCycle);
        await _stakerFixtures.ClearData(DbCollectionName.Stakers);
        await _transactionFixtures.ClearData(DbCollectionName.Transactions);
        await _balanceChangeFixtures.ClearData(DbCollectionName.Balance);
        await _chilledFixtures.ClearData(DbCollectionName.Chilled);
        await _signedEffectiveBalanceFixtures.ClearData(DbCollectionName.SignEffectiveBalance);
    }

    public IMongoCollection<EffectiveBalanceModel> EffectiveBalanceCollection
    {
        get
        {
            return _effectiveBalanceCollection ??=
                _database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
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
    
    public IMongoCollection<ChilledModel> ChilledCollection
    {
        get { return _chilledCollection ??= _database.GetCollection<ChilledModel>(DbCollectionName.Chilled); }
    }

    public IMongoCollection<SignedEffectiveBalanceModel> SignedEffectiveBalanceCollection
    {
        get { return _signedEffectiveBalanceCollection ??= _database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance); }
    }

    public void Dispose()
    {
    }
}