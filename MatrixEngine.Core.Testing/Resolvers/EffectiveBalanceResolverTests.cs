using System.Numerics;
using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Engine;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatrixEngine.Core.Testing.Resolvers;

public class EffectiveBalanceResolverTests
{
    private readonly EffectiveBalanceResolver _effectiveBalanceResolver;
    private readonly Mock<IEffectiveBalanceService> _effectiveBalanceService;
    private Mock<ILogger<EffectiveBalanceResolver>> _logger;

    public EffectiveBalanceResolverTests()
    {
        _logger = new Mock<ILogger<EffectiveBalanceResolver>>();
        _effectiveBalanceService = new Mock<IEffectiveBalanceService>();
        _effectiveBalanceResolver = new EffectiveBalanceResolver(_effectiveBalanceService.Object, _logger.Object);
    }

    [Fact]
    public void SaveEffectiveBalances_WhenCalled_CallsUpsertEffectiveBalance()
    {
        // Arrange
        var effectiveBalances = new List<EffectiveBalanceModel>
        {
            new()
            {
                Account = "0x001",
                EraIndex = 1,
                EffectiveEras = 1,
                EffectiveBalance = "100" 
            }
        };

        // Act
        _effectiveBalanceResolver.SaveEffectiveBalances(effectiveBalances);

        // Assert
        _effectiveBalanceService.Verify(x => x.UpsertEffectiveBalance(effectiveBalances), Times.Once);
    }
    
    [Fact]
    public void CalculateOneAccountEffectiveBalance_WhenCalled_ReturnsEffectiveBalance()
    {
        // Arrange
        const string account = "0x001";

        var appliedPunishmentBalanceChanges = new List<BalanceChangeModel>
        {
            new()
            {
                Account = account,
                StartBlock = 171,
                EndBlock = 200,
                EraIndex = 1,
                EffectiveBlocks = 30,
                EffectiveEras = 30m / 101m,
                BalanceChange = 50,
                BalanceInBlockRange = 100,
                StakerType = StakerType.Staker,
            },
            new()
            {
                Account = account,
                StartBlock = 151,
                EndBlock = 170,
                EraIndex = 1,
                EffectiveBlocks = 20,
                EffectiveEras = 20m / 101m,
                BalanceChange = -50,
                BalanceInBlockRange = 50,
                StakerType = StakerType.Staker,
            },
            new()
            {
                Account = account,
                StartBlock = 100,
                EndBlock = 150,
                EraIndex = 1,
                EffectiveBlocks = 51,
                EffectiveEras = 51m / 101m,
                BalanceChange = 100,
                BalanceInBlockRange = 50,
                StakerType = StakerType.Staker,
            },
            new()
            {
                Account = account,
                StartBlock = 0,
                EndBlock = 99,
                EraIndex = 0,
                EffectiveBlocks = 100,
                EffectiveEras = 100m / 100m,
                BalanceChange = 100,
                BalanceInBlockRange = 50,
                StakerType = StakerType.Staker,
            }
        };

        var effectiveBalances =
            _effectiveBalanceResolver.CalculateOneAccountEffectiveBalance(account, appliedPunishmentBalanceChanges);

        // Assert
        Assert.Equal(4, effectiveBalances.Count);
    }

    [Fact]
    public void CalculateEffectiveBalanceWithPrecisions_WhenEffectiveEraIsOne()
    {
        //use existing prod data
        const decimal effectiveEras = 1m;
        BigInteger totalBalance = 50000000000;
        var effectiveBalance =
            _effectiveBalanceResolver.CalculateEffectiveBalanceWithPrecisions(effectiveEras, totalBalance);

        Assert.Equal(555555555, effectiveBalance);
    }

    [Fact]
    public void CalculateEffectiveBalanceWithPrecisions_WhenEffectiveEraIsFaction_1()
    {
        //use existing prod data
        const decimal effectiveEras = 0.6293123408196342m;
        BigInteger totalBalance = 50000000000;
        var effectiveBalance =
            _effectiveBalanceResolver.CalculateEffectiveBalanceWithPrecisions(effectiveEras, totalBalance);

        Assert.Equal(349617965, effectiveBalance);
    }

    [Fact]
    public void CalculateEffectiveBalanceWithPrecisions_WhenEffectiveEraIsFaction_2()
    {
        //use existing prod data
        const decimal effectiveEras = 0.3707339661958787m;
        BigInteger totalBalance = 50000000000;
        var effectiveBalance =
            _effectiveBalanceResolver.CalculateEffectiveBalanceWithPrecisions(effectiveEras, totalBalance);

        Assert.Equal(205963310, effectiveBalance);
    }
}