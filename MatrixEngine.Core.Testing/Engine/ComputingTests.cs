using MatrixEngine.Core.Engine;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Resolvers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatrixEngine.Core.Testing.Engine;

public class ComputingTests
{
    private readonly Mock<IRewardCycleResolver> _rewardCycleResolver;
    private readonly Mock<IBalanceChangeResolver> _balanceChangeResolver;
    private readonly Mock<IEffectiveBalanceResolver> _effectiveBalanceResolver;
    private readonly Mock<ILogger<ComputingCore>> _loggerMock;
    private readonly ComputingCore _computingCore;
    private readonly Mock<IBalanceSnapshotResolver> _balanceSnapshotResolver;

    public ComputingTests()
    {
        _rewardCycleResolver = new Mock<IRewardCycleResolver>();
        _balanceSnapshotResolver = new Mock<IBalanceSnapshotResolver>();
        _balanceChangeResolver = new Mock<IBalanceChangeResolver>();
        _effectiveBalanceResolver = new Mock<IEffectiveBalanceResolver>();
        
        _loggerMock = new Mock<ILogger<ComputingCore>>();
        _computingCore = new Core.Engine.ComputingCore(_rewardCycleResolver.Object,
            _balanceSnapshotResolver.Object,
            _balanceChangeResolver.Object,
            _effectiveBalanceResolver.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Start_ShouldNotCallBalanceChangeResolver_WhenNoRewardCycle()
    {
        _rewardCycleResolver.Setup(m => m.GetToBeCalculatedCycles()).ReturnsAsync((List<RewardCycle>?)null);

        await _computingCore.Compute();

        _balanceChangeResolver.Verify(m => m.ResolveBalanceChange(It.IsAny<Int32>(), It.IsAny<Int32>()), Times.Never);
    }

    [Fact]
    public async Task Start_ShouldNotCall_EffectiveBalanceResolver_WhenNoRewardCycle()
    {
        _rewardCycleResolver.Setup(m => m.GetToBeCalculatedCycles()).ReturnsAsync((List<RewardCycle>?)null);

        await _computingCore.Compute();
        _effectiveBalanceResolver.Verify(
            m => m.CalculateEffectiveBalances(It.IsAny<Dictionary<string, List<BalanceChangeModel>>>()),
            Times.Never);
    }

    [Fact]
    public async Task Start_ShouldCallBalanceChangeResolverOnce_WhenRewardCycleExists()
    {
        _rewardCycleResolver.Setup(m => m.GetToBeCalculatedCycles()).ReturnsAsync(new List<RewardCycle>
        {
            new RewardCycle
            {
                StartBlock = 1,
                EndBlock = 10
            }
        });

        _rewardCycleResolver.Setup(m => 
                m.GetRewardCycleEraIndexRangeByBlockRange(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new Tuple<int, int>(1, 1));
        await _computingCore.Compute();

        _balanceChangeResolver.Verify(m => m.ResolveBalanceChange(1, 10), Times.Once);
    }

    [Fact]
    public async Task Start_ShouldCallEffectiveBalanceResolverOnce_WhenRewardCycleExists()
    {
        _rewardCycleResolver.Setup(m => m.GetToBeCalculatedCycles()).ReturnsAsync(new List<RewardCycle>
        {
            new RewardCycle
            {
                StartBlock = 1,
                EndBlock = 10
            }
        });

        _rewardCycleResolver.Setup(m => 
                m.GetRewardCycleEraIndexRangeByBlockRange(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new Tuple<int, int>(1, 1));
        
        await _computingCore.Compute();

        _effectiveBalanceResolver.Verify(
            m => m.CalculateEffectiveBalances(It.IsAny<Dictionary<string, List<BalanceChangeModel>>>()),
            Times.Once);
    }

    [Fact]
    public async Task Start_ShouldCallBalanceChangeResolverTwice_WhenRewardCycleExists()
    {
        _rewardCycleResolver.Setup(m => m.GetToBeCalculatedCycles()).ReturnsAsync(new List<RewardCycle>
        {
            new RewardCycle
            {
                StartBlock = 1,
                EndBlock = 10
            },
            new RewardCycle
            {
                StartBlock = 11,
                EndBlock = 20
            }
        });

        _rewardCycleResolver.Setup(m => 
                m.GetRewardCycleEraIndexRangeByBlockRange(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new Tuple<int, int>(1, 2));
        await _computingCore.Compute();

        _balanceChangeResolver.Verify(m => m.ResolveBalanceChange(1, 10), Times.Once);
        _balanceChangeResolver.Verify(m => m.ResolveBalanceChange(11, 20), Times.Once);
    }

    [Fact]
    public async Task Start_ShouldCallEffectiveBalanceResolverTwice_WhenRewardCycleExists()
    {
        _rewardCycleResolver.Setup(m => m.GetToBeCalculatedCycles()).ReturnsAsync(new List<RewardCycle>
        {
            new RewardCycle
            {
                StartBlock = 1,
                EndBlock = 10
            },
            new RewardCycle
            {
                StartBlock = 11,
                EndBlock = 20
            }
        });

        _rewardCycleResolver.Setup(m => 
                m.GetRewardCycleEraIndexRangeByBlockRange(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new Tuple<int, int>(1, 2));
        
        await _computingCore.Compute();

        _effectiveBalanceResolver.Verify(
            m => m.CalculateEffectiveBalances(It.IsAny<Dictionary<string, List<BalanceChangeModel>>>()),
            Times.Exactly(2));
    }
}