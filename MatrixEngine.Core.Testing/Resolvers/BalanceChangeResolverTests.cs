using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Engine;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.Events;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Testing.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatrixEngine.Core.Testing.Resolvers;

public class BalanceChangeResolverTests
{
    private readonly BalanceChangeResolver _balanceChangeResolver;
    private readonly Mock<IBalanceSnapshotService> _balanceSnapshotServiceMock;
    private readonly Mock<IEraService> _eraServiceMock;
    private readonly Mock<ITransactionEventService> _transactionServiceMock;
    private readonly Mock<IStakerService> _stakerServiceMock;
    private readonly Mock<ILogger<BalanceChangeResolver>> _logger;
    private readonly Mock<IBalanceChangeService> _balanceChangeService;

    public BalanceChangeResolverTests()
    {
        _logger = new Mock<ILogger<BalanceChangeResolver>>();
        _balanceSnapshotServiceMock = new Mock<IBalanceSnapshotService>();
        _eraServiceMock = new Mock<IEraService>();
        _transactionServiceMock = new Mock<ITransactionEventService>();
        _stakerServiceMock = new Mock<IStakerService>();
        _stakerServiceMock.Setup(m => m.GetAccountStakerTypesByEraIndexes(It.IsAny<string>(), It.IsAny<List<int>>()))
            .ReturnsAsync(new List<StakerModel>());
        _balanceChangeService = new Mock<IBalanceChangeService>();

        _balanceChangeResolver = new BalanceChangeResolver(_balanceSnapshotServiceMock.Object, _eraServiceMock.Object,
            _transactionServiceMock.Object, _stakerServiceMock.Object, _balanceChangeService.Object, _logger.Object);
        _stakerServiceMock.Setup(
                m =>
                    m.GetAccountsStakerTypesByEraIndexes(new List<string> { "0x001" }, It.IsAny<List<int>>()))
            .ReturnsAsync(new List<StakerModel>
            {
                new StakerModel()
                {
                    Account = "0x001",
                    EraIndex = 0,
                    Type = StakerType.Staker,
                }
            });
    }

    [Fact]
    public async Task ResolveBalanceChange_ReturnSingleResult()
    {
        var transactions =
            JsonFileReader.Read<List<TransactionModel>>(@"./Data/transactions/single-account-bonded.json");
        _transactionServiceMock.Setup(m => m.GetTransactionEventsByBlockRange(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(transactions);
        _balanceSnapshotServiceMock.Setup(m => m.GetBalanceSnapshotByCycleEndBlock(It.IsAny<int>()))
            .ReturnsAsync(new List<BalanceSnapshotModel>());

        var eras = new List<EraModel>
        {
            new()
            {
                EraIndex = 0,
                StartBlock = 0,
                EndBlock = 99,
            },
            new()
            {
                EraIndex = 1,
                StartBlock = 100,
                EndBlock = 200,
            },
        };
        _eraServiceMock.Setup(m => m.GetEraListByBlockRange(0, 200)).ReturnsAsync(
            eras
        );

        var balanceChange =
            await _balanceChangeResolver.ResolveBalanceChange(0, 200);

        Assert.Single(balanceChange.Keys);
    }

    [Fact]
    public async Task CalculateBalanceChanges_WhenSingleAccount_AndOnlyBonded()
    {
        var transactions =
            JsonFileReader.Read<List<TransactionModel>>(@"./Data/transactions/single-account-bonded.json");
        var balanceChange =
            await _balanceChangeResolver.CalculateBalanceChanges(new List<BalanceSnapshotModel>(), transactions, 0, 200);
        Assert.Single(balanceChange.Keys);
        var account = balanceChange.Keys.First();
        Assert.Equal("0x001", account);

        var balanceChanges = balanceChange[account];
        Assert.Equal(3, balanceChanges.Count);

        Assert.Equal(0, balanceChanges[0].StartBlock);
        Assert.Equal(49, balanceChanges[0].EndBlock);
        Assert.Equal(0, balanceChanges[0].PreviousBalance);
        Assert.Equal(100, balanceChanges[0].BalanceChange);
        Assert.Equal(100, balanceChanges[0].BalanceInBlockRange);

        Assert.Equal(50, balanceChanges[1].StartBlock);
        Assert.Equal(149, balanceChanges[1].EndBlock);
        Assert.Equal(100, balanceChanges[1].PreviousBalance);
        Assert.Equal(100, balanceChanges[1].BalanceChange);
        Assert.Equal(200, balanceChanges[1].BalanceInBlockRange);

        Assert.Equal(150, balanceChanges[2].StartBlock);
        Assert.Equal(200, balanceChanges[2].EndBlock);
        Assert.Equal(200, balanceChanges[2].PreviousBalance);
        Assert.Equal(100, balanceChanges[2].BalanceChange);
        Assert.Equal(300, balanceChanges[2].BalanceInBlockRange);
    }

    [Fact]
    public async Task CalculateBalanceChanges_WhenSingleAccount_AndOnlyBonded_WithBalanceSnapshot()
    {
        var transactions =
            JsonFileReader.Read<List<TransactionModel>>(@"./Data/transactions/single-account-bonded.json");
        var balanceSnapshots = new List<BalanceSnapshotModel>
        {
            new()
            {
                Account = "0x001",
                Balance = "100",
            },
        };
        var balanceChange =
            await _balanceChangeResolver.CalculateBalanceChanges(balanceSnapshots, transactions, 0, 200);

        Assert.Single(balanceChange.Keys);
        var account = balanceChange.Keys.First();
        Assert.Equal("0x001", account);

        var balanceChanges = balanceChange[account];
        Assert.Equal(3, balanceChanges.Count);

        Assert.Equal(0, balanceChanges[0].StartBlock);
        Assert.Equal(49, balanceChanges[0].EndBlock);
        Assert.Equal(100, balanceChanges[0].PreviousBalance); //balance snapshot
        Assert.Equal(100, balanceChanges[0].BalanceChange);
        Assert.Equal(200, balanceChanges[0].BalanceInBlockRange);

        Assert.Equal(50, balanceChanges[1].StartBlock);
        Assert.Equal(149, balanceChanges[1].EndBlock);
        Assert.Equal(200, balanceChanges[1].PreviousBalance);
        Assert.Equal(100, balanceChanges[1].BalanceChange);
        Assert.Equal(300, balanceChanges[1].BalanceInBlockRange);

        Assert.Equal(150, balanceChanges[2].StartBlock);
        Assert.Equal(200, balanceChanges[2].EndBlock);
        Assert.Equal(300, balanceChanges[2].PreviousBalance);
        Assert.Equal(100, balanceChanges[2].BalanceChange);
        Assert.Equal(400, balanceChanges[2].BalanceInBlockRange);
    }

    [Fact]
    public async Task CalculateBalanceChanges_WhenSingleAccount_BondedAndWithdrawn()
    {
        var transactions =
            JsonFileReader.Read<List<TransactionModel>>(@"./Data/transactions/single-account-bonded-withdrawn.json");
        var balanceChange =
            await _balanceChangeResolver.CalculateBalanceChanges(new List<BalanceSnapshotModel>(), transactions, 0, 300);
        Assert.Single(balanceChange.Keys);
        var account = balanceChange.Keys.First();
        Assert.Equal("0x001", account);

        var balanceChanges = balanceChange[account];
        Assert.Equal(5, balanceChanges.Count);

        Assert.Equal(0, balanceChanges[0].StartBlock);
        Assert.Equal(49, balanceChanges[0].EndBlock);
        Assert.Equal(0, balanceChanges[0].PreviousBalance);
        Assert.Equal(100, balanceChanges[0].BalanceChange);
        Assert.Equal(100, balanceChanges[0].BalanceInBlockRange);

        Assert.Equal(50, balanceChanges[1].StartBlock);
        Assert.Equal(149, balanceChanges[1].EndBlock);
        Assert.Equal(100, balanceChanges[1].PreviousBalance);
        Assert.Equal(100, balanceChanges[1].BalanceChange);
        Assert.Equal(200, balanceChanges[1].BalanceInBlockRange);

        Assert.Equal(150, balanceChanges[2].StartBlock);
        Assert.Equal(169, balanceChanges[2].EndBlock);
        Assert.Equal(200, balanceChanges[2].PreviousBalance);
        Assert.Equal(100, balanceChanges[2].BalanceChange);
        Assert.Equal(300, balanceChanges[2].BalanceInBlockRange);

        Assert.Equal(170, balanceChanges[3].StartBlock);
        Assert.Equal(199, balanceChanges[3].EndBlock);
        Assert.Equal(300, balanceChanges[3].PreviousBalance);
        Assert.Equal(-20, balanceChanges[3].BalanceChange);
        Assert.Equal(280, balanceChanges[3].BalanceInBlockRange);

        Assert.Equal(200, balanceChanges[4].StartBlock);
        Assert.Equal(300, balanceChanges[4].EndBlock);
        Assert.Equal(280, balanceChanges[4].PreviousBalance);
        Assert.Equal(100, balanceChanges[4].BalanceChange);
        Assert.Equal(380, balanceChanges[4].BalanceInBlockRange);
    }

    [Fact]
    public async Task CalculateBalanceChanges_WhenSingleAccount_BondedAndWithdrawn_WithBalanceSnapshot()
    {
        var transactions =
            JsonFileReader.Read<List<TransactionModel>>(@"./Data/transactions/single-account-bonded-withdrawn.json");
        var balanceSnapshots = new List<BalanceSnapshotModel>
        {
            new()
            {
                Account = "0x001",
                Balance = "100",
            },
        };
        var balanceChange =
            await _balanceChangeResolver.CalculateBalanceChanges(balanceSnapshots, transactions, 0, 300);
        Assert.Single(balanceChange.Keys);
        var account = balanceChange.Keys.First();
        Assert.Equal("0x001", account);

        var balanceChanges = balanceChange[account];
        Assert.Equal(5, balanceChanges.Count);

        Assert.Equal(0, balanceChanges[0].StartBlock);
        Assert.Equal(49, balanceChanges[0].EndBlock);
        Assert.Equal(100, balanceChanges[0].PreviousBalance); //balance snapshot
        Assert.Equal(100, balanceChanges[0].BalanceChange);
        Assert.Equal(200, balanceChanges[0].BalanceInBlockRange);

        Assert.Equal(50, balanceChanges[1].StartBlock);
        Assert.Equal(149, balanceChanges[1].EndBlock);
        Assert.Equal(200, balanceChanges[1].PreviousBalance);
        Assert.Equal(100, balanceChanges[1].BalanceChange);
        Assert.Equal(300, balanceChanges[1].BalanceInBlockRange);

        Assert.Equal(150, balanceChanges[2].StartBlock);
        Assert.Equal(169, balanceChanges[2].EndBlock);
        Assert.Equal(300, balanceChanges[2].PreviousBalance);
        Assert.Equal(100, balanceChanges[2].BalanceChange);
        Assert.Equal(400, balanceChanges[2].BalanceInBlockRange);

        Assert.Equal(170, balanceChanges[3].StartBlock);
        Assert.Equal(199, balanceChanges[3].EndBlock);
        Assert.Equal(400, balanceChanges[3].PreviousBalance);
        Assert.Equal(-20, balanceChanges[3].BalanceChange);
        Assert.Equal(380, balanceChanges[3].BalanceInBlockRange);

        Assert.Equal(200, balanceChanges[4].StartBlock);
        Assert.Equal(300, balanceChanges[4].EndBlock);
        Assert.Equal(380, balanceChanges[4].PreviousBalance);
        Assert.Equal(100, balanceChanges[4].BalanceChange);
        Assert.Equal(480, balanceChanges[4].BalanceInBlockRange);
    }

    [Fact]
    public async Task CalculateBalanceChanges_CalledUpsertBalanceChanges()
    {
        var transactions =
            JsonFileReader.Read<List<TransactionModel>>(@"./Data/transactions/single-account-bonded-withdrawn.json");
        var balanceChange =
            await _balanceChangeResolver.CalculateBalanceChanges(new List<BalanceSnapshotModel>(), transactions, 0,
                300);
        
        _balanceChangeService.Verify(m => m.UpsertUserBalanceChanges(It.IsAny<List<BalanceChangeModel>>()), Times.Once);
    }

    [Fact]
    public async Task SplitBalanceChangesAcrossEras_WhenOnlyOneChangeCrossTwoEras()
    {
        var balanceChanges = new Dictionary<string, List<BalanceChangeModel>>();
        balanceChanges["0x001"] = new List<BalanceChangeModel>
        {
            new BalanceChangeModel
            {
                Account = "0x001",
                BalanceChange = 100,
                BalanceInBlockRange = 100,
                StartBlock = 0,
                EndBlock = 150
            },
        };

        var eras = new List<EraModel>
        {
            new EraModel
            {
                EraIndex = 0,
                StartBlock = 0,
                EndBlock = 99,
            },
            new EraModel
            {
                EraIndex = 1,
                StartBlock = 100,
                EndBlock = 200,
            },
        };

        var adjustedBalanceChanges = await _balanceChangeResolver.SplitBalanceChangesAcrossEras(balanceChanges, eras);
        Assert.Single(adjustedBalanceChanges.Keys);
        var account = adjustedBalanceChanges.Keys.First();
        Assert.Equal("0x001", account);

        var balanceChangesForAccount = adjustedBalanceChanges[account];
        Assert.Equal(2, balanceChangesForAccount.Count);

        Assert.Equal(0, balanceChangesForAccount[0].StartBlock);
        Assert.Equal(99, balanceChangesForAccount[0].EndBlock);
        Assert.Equal(100, balanceChangesForAccount[0].BalanceChange);
        Assert.Equal(100, balanceChangesForAccount[0].BalanceInBlockRange);
        Assert.Equal(100, balanceChangesForAccount[0].EffectiveBlocks);
        Assert.Equal(0, balanceChangesForAccount[0].EraIndex);
        Assert.Equal(StakerType.Staker, balanceChangesForAccount[0].StakerType);

        Assert.Equal(100, balanceChangesForAccount[1].StartBlock);
        Assert.Equal(200, balanceChangesForAccount[1].EndBlock);
        Assert.Equal(100, balanceChangesForAccount[1].BalanceChange);
        Assert.Equal(50, balanceChangesForAccount[1].BalanceInBlockRange);
        Assert.Equal(101, balanceChangesForAccount[1].EffectiveBlocks);
        Assert.Equal(1, balanceChangesForAccount[1].EraIndex);
        Assert.Equal(StakerType.Staker, balanceChangesForAccount[0].StakerType);
    }

    [Fact]
    public async Task SplitBalanceChangesAcrossEras_WhenStakerTypeIsValidator()
    {
        var balanceChanges = new Dictionary<string, List<BalanceChangeModel>>();
        balanceChanges["0x001"] = new List<BalanceChangeModel>
        {
            new BalanceChangeModel
            {
                Account = "0x001",
                BalanceChange = 100,
                BalanceInBlockRange = 100,
                StartBlock = 0,
                EndBlock = 150
            },
        };

        var eras = new List<EraModel>
        {
            new EraModel
            {
                EraIndex = 0,
                StartBlock = 0,
                EndBlock = 99,
            },
            new EraModel
            {
                EraIndex = 1,
                StartBlock = 100,
                EndBlock = 200,
            },
        };

        _stakerServiceMock.Setup(
                m =>
                    m.GetAccountsStakerTypesByEraIndexes(new List<string> { "0x001" }, It.IsAny<List<int>>()))
            .ReturnsAsync(new List<StakerModel>
            {
                new StakerModel()
                {
                    Account = "0x001",
                    EraIndex = 0,
                    Type = StakerType.Validator,
                }
            });
        var adjustedBalanceChanges = await _balanceChangeResolver.SplitBalanceChangesAcrossEras(balanceChanges, eras);
        Assert.Single(adjustedBalanceChanges.Keys);
        var account = adjustedBalanceChanges.Keys.First();
        Assert.Equal("0x001", account);

        var balanceChangesForAccount = adjustedBalanceChanges[account];
        Assert.Equal(2, balanceChangesForAccount.Count);

        Assert.Equal(0, balanceChangesForAccount[0].StartBlock);
        Assert.Equal(99, balanceChangesForAccount[0].EndBlock);
        Assert.Equal(100, balanceChangesForAccount[0].BalanceChange);
        Assert.Equal(100, balanceChangesForAccount[0].BalanceInBlockRange);
        Assert.Equal(100, balanceChangesForAccount[0].EffectiveBlocks);
        Assert.Equal(0, balanceChangesForAccount[0].EraIndex);
        Assert.Equal(StakerType.Validator, balanceChangesForAccount[0].StakerType);

        Assert.Equal(100, balanceChangesForAccount[1].StartBlock);
        Assert.Equal(200, balanceChangesForAccount[1].EndBlock);
        Assert.Equal(100, balanceChangesForAccount[1].BalanceChange);
        Assert.Equal(50, balanceChangesForAccount[1].BalanceInBlockRange);
        Assert.Equal(101, balanceChangesForAccount[1].EffectiveBlocks);
        Assert.Equal(1, balanceChangesForAccount[1].EraIndex);
        Assert.Equal(StakerType.Staker, balanceChangesForAccount[1].StakerType);
    }

    [Fact]
    public async Task SplitBalanceChangesAcrossEras_WhenOnlyOneChangeCrossTwoEras_AndMoreChange()
    {
        var balanceChanges = new Dictionary<string, List<BalanceChangeModel>>();
        balanceChanges["0x001"] = new List<BalanceChangeModel>
        {
            new BalanceChangeModel
            {
                Account = "0x001",
                BalanceChange = 100,
                BalanceInBlockRange = 100,
                StartBlock = 0,
                EndBlock = 150
            },
            new BalanceChangeModel
            {
                Account = "0x001",
                BalanceChange = 100,
                BalanceInBlockRange = 200,
                StartBlock = 151,
                EndBlock = 200
            },
        };

        var eras = new List<EraModel>
        {
            new EraModel
            {
                EraIndex = 0,
                StartBlock = 0,
                EndBlock = 99,
            },
            new EraModel
            {
                EraIndex = 1,
                StartBlock = 100,
                EndBlock = 200,
            },
        };

        var adjustedBalanceChanges = await _balanceChangeResolver.SplitBalanceChangesAcrossEras(balanceChanges, eras);
        Assert.Single(adjustedBalanceChanges.Keys);
        var account = adjustedBalanceChanges.Keys.First();
        Assert.Equal("0x001", account);

        var balanceChangesForAccount = adjustedBalanceChanges[account];
        Assert.Equal(2, balanceChangesForAccount.Count);

        Assert.Equal(0, balanceChangesForAccount[0].StartBlock);
        Assert.Equal(99, balanceChangesForAccount[0].EndBlock);
        Assert.Equal(100, balanceChangesForAccount[0].BalanceChange);
        Assert.Equal(100, balanceChangesForAccount[0].BalanceInBlockRange);
        Assert.Equal(100, balanceChangesForAccount[0].EffectiveBlocks);
        Assert.Equal(1, balanceChangesForAccount[0].EffectiveEras);
        Assert.Equal(0, balanceChangesForAccount[0].EraIndex);

        Assert.Equal(100, balanceChangesForAccount[1].StartBlock);
        Assert.Equal(200, balanceChangesForAccount[1].EndBlock);
        Assert.Equal(200, balanceChangesForAccount[1].BalanceChange);
        Assert.Equal(149, balanceChangesForAccount[1].BalanceInBlockRange);
        Assert.Equal(101, balanceChangesForAccount[1].EffectiveBlocks);
        Assert.Equal(1, balanceChangesForAccount[1].EffectiveEras);
        Assert.Equal(1, balanceChangesForAccount[1].EraIndex);

    }

    [Fact]
    public async Task ApplyPunishmentForBalanceChanges_WhenWithDrawnAtThenEnd()
    {
        var balanceChanges = new Dictionary<string, List<BalanceChangeModel>>();
        balanceChanges["0x001"] = new List<BalanceChangeModel>
        {
            new BalanceChangeModel
            {
                Account = "0x001",
                BalanceChange = 100,
                BalanceInBlockRange = 100,
                StartBlock = 0,
                EndBlock = 150
            },
            new BalanceChangeModel
            {
                Account = "0x001",
                BalanceChange = -50,
                BalanceInBlockRange = 50,
                StartBlock = 151,
                EndBlock = 200
            },
        };

        var eras = new List<EraModel>
        {
            new EraModel
            {
                EraIndex = 0,
                StartBlock = 0,
                EndBlock = 99,
            },
            new EraModel
            {
                EraIndex = 1,
                StartBlock = 100,
                EndBlock = 200,
            },
        };

        var adjustedBalanceChanges = await _balanceChangeResolver.SplitBalanceChangesAcrossEras(balanceChanges, eras);

        var applyPunishmentForBalanceChanges =
            _balanceChangeResolver.ApplyPunishmentForBalanceChanges(adjustedBalanceChanges);
        Assert.Single(applyPunishmentForBalanceChanges.Keys);

        var account = applyPunishmentForBalanceChanges.Keys.First();
        Assert.Equal("0x001", account);

        var balanceChangesForAccount = applyPunishmentForBalanceChanges[account];

        Assert.Equal(2, balanceChangesForAccount.Count);

        Assert.Equal(100, balanceChangesForAccount[0].StartBlock);
        Assert.Equal(200, balanceChangesForAccount[0].EndBlock);
        Assert.Equal(50, balanceChangesForAccount[0].BalanceChange);
        Assert.Equal(49, balanceChangesForAccount[0].BalanceInBlockRange);

        Assert.Equal(0, balanceChangesForAccount[1].StartBlock);
        Assert.Equal(99, balanceChangesForAccount[1].EndBlock);
        Assert.Equal(100, balanceChangesForAccount[1].BalanceChange);
        Assert.Equal(49, balanceChangesForAccount[1].BalanceInBlockRange);
    }

    [Fact]
    public async Task ApplyPunishmentForBalanceChanges_WhenWithDrawnInTheMiddle()
    {
        var balanceChanges = new Dictionary<string, List<BalanceChangeModel>>();
        balanceChanges["0x001"] = new List<BalanceChangeModel>
        {
            new()
            {
                Account = "0x001",
                BalanceChange = 100,
                BalanceInBlockRange = 100,
                StartBlock = 0,
                EndBlock = 150
            },
            new()
            {
                Account = "0x001",
                BalanceChange = -50,
                BalanceInBlockRange = 50,
                StartBlock = 151,
                EndBlock = 170,
            },
            new()
            {
                Account = "0x001",
                BalanceChange = 50,
                BalanceInBlockRange = 100,
                StartBlock = 171,
                EndBlock = 200,
            },
        };

        var eras = new List<EraModel>
        {
            new EraModel
            {
                EraIndex = 0,
                StartBlock = 0,
                EndBlock = 99,
            },
            new EraModel
            {
                EraIndex = 1,
                StartBlock = 100,
                EndBlock = 200,
            },
        };

        var adjustedBalanceChanges = await _balanceChangeResolver.SplitBalanceChangesAcrossEras(balanceChanges, eras);

        var applyPunishmentForBalanceChanges =
            _balanceChangeResolver.ApplyPunishmentForBalanceChanges(adjustedBalanceChanges);
        Assert.Single(applyPunishmentForBalanceChanges.Keys);

        var account = applyPunishmentForBalanceChanges.Keys.First();
        Assert.Equal("0x001", account);

        var balanceChangesForAccount = applyPunishmentForBalanceChanges[account];

        Assert.Equal(2, balanceChangesForAccount.Count);

        Assert.Equal(100, balanceChangesForAccount[0].StartBlock);
        Assert.Equal(200, balanceChangesForAccount[0].EndBlock);
        Assert.Equal(1, balanceChangesForAccount[0].EraIndex);
        Assert.Equal(101, balanceChangesForAccount[0].EffectiveBlocks);
        Assert.Equal(1, balanceChangesForAccount[0].EffectiveEras);
        Assert.Equal(100, balanceChangesForAccount[0].BalanceChange);
        Assert.Equal(64, balanceChangesForAccount[0].BalanceInBlockRange);

        Assert.Equal(0, balanceChangesForAccount[1].StartBlock);
        Assert.Equal(99, balanceChangesForAccount[1].EndBlock);
        Assert.Equal(0, balanceChangesForAccount[1].EraIndex);
        Assert.Equal(100, balanceChangesForAccount[1].EffectiveBlocks);
        Assert.Equal(1, balanceChangesForAccount[1].EffectiveEras);
        Assert.Equal(100, balanceChangesForAccount[1].BalanceChange);
        Assert.Equal(64, balanceChangesForAccount[1].BalanceInBlockRange);
    }

    [Fact]
    public void UserAdjustedBalanceChanges_WhenUsersBalanceChangeCrossEras()
    {
        var balanceChanges = new Dictionary<string, List<BalanceChangeModel>>
        {
            ["0x001"] = new()
            {
                new()
                {
                    Account = "0x001",
                    BalanceChange = 100,
                    BalanceInBlockRange = 100,
                    StartBlock = 0,
                    EndBlock = 150
                },
                new()
                {
                    Account = "0x001",
                    BalanceChange = -50,
                    BalanceInBlockRange = 50,
                    StartBlock = 151,
                    EndBlock = 170,
                },
                new()
                {
                    Account = "0x001",
                    BalanceChange = 50,
                    BalanceInBlockRange = 100,
                    StartBlock = 171,
                    EndBlock = 200,
                },
            }
        };

        var eras = new List<EraModel>
        {
            new()
            {
                EraIndex = 0,
                StartBlock = 0,
                EndBlock = 99,
            },
            new()
            {
                EraIndex = 1,
                StartBlock = 100,
                EndBlock = 200,
            }
        };

        const string account = "0x001";
        
        var userAdjustedBalanceChanges = _balanceChangeResolver.UserAdjustedBalanceChanges(
            eras, 
            new KeyValuePair<string, List<BalanceChangeModel>>( "0x001", balanceChanges["0x001"]),
            account, new List<StakerModel>());
        
        Assert.Equal(2, userAdjustedBalanceChanges.Count);
        
        Assert.Equal(100, userAdjustedBalanceChanges[0].BalanceChange);
        Assert.Equal(100, userAdjustedBalanceChanges[0].BalanceInBlockRange);
        Assert.Equal(100, userAdjustedBalanceChanges[0].EffectiveBlocks);
        Assert.Equal(0, userAdjustedBalanceChanges[0].EraIndex);
        Assert.Equal(StakerType.Staker, userAdjustedBalanceChanges[0].StakerType);
        
        Assert.Equal(100, userAdjustedBalanceChanges[1].BalanceChange);
        Assert.Equal(64, userAdjustedBalanceChanges[1].BalanceInBlockRange);
        Assert.Equal(101, userAdjustedBalanceChanges[1].EffectiveBlocks);
        Assert.Equal(1, userAdjustedBalanceChanges[1].EraIndex);
        Assert.Equal(StakerType.Staker, userAdjustedBalanceChanges[1].StakerType);
    }
}