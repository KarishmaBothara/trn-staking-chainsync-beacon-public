using System.Numerics;
using MatrixEngine.Core.Exceptions;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.DTOs;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatrixEngine.Core.Testing.Resolvers;

public class BalanceSnapshotResolverTests
{
    private readonly Mock<IRewardCycleService> _rewardCycleService;
    private readonly Mock<IBalanceSnapshotService> _balanceSnapshotService;
    private readonly Mock<IGenesisValidatorService> _genesisValidatorService;
    private readonly Mock<IBalanceChangeService> _balanceChangeService;
    private readonly Mock<ILogger<BalanceSnapshotResolver>> _logger;
    private readonly BalanceSnapshotResolver _balanceSnapshotResolver;

    public BalanceSnapshotResolverTests()
    {
        _rewardCycleService = new Mock<IRewardCycleService>();
        _balanceSnapshotService = new Mock<IBalanceSnapshotService>();
        _genesisValidatorService = new Mock<IGenesisValidatorService>();
        _balanceChangeService = new Mock<IBalanceChangeService>();
        _logger = new Mock<ILogger<BalanceSnapshotResolver>>();
        _balanceSnapshotResolver = new BalanceSnapshotResolver(
            _rewardCycleService.Object,
            _balanceSnapshotService.Object,
            _genesisValidatorService.Object,
            _balanceChangeService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task CalculateGenesisBalanceSnapshot_WhenGenesisValidatorExists()
    {
        var cycle = new RewardCycle
        {
            StartBlock = 7035346,
            EndBlock = 7035347
        };

        _genesisValidatorService.Setup(x => x.GetGenesisValidators()).ReturnsAsync(new List<GenesisValidatorModel>
        {
            new()
            {
                Stash = "0xA76bFE6d90952Eb0a3b8fA31dF2580692acEf772",
                LockedBalance = "2000000"
            },
            new()
            {
                Stash = "0xA76bFE6d90952Eb0a3b8fA31dF2580692acEf772",
                LockedBalance = "3000000"
            }
        });

        await _balanceSnapshotResolver.CalculateGenesisBalanceSnapshot(cycle);

        _genesisValidatorService.Verify(x => x.GetGenesisValidators(), Times.Once);
        _balanceSnapshotService.Verify(x =>
                x.UpsertBalanceSnapshots(It.Is<List<BalanceSnapshotModel>>(
                    b =>
                        b.Exists(a =>
                            (a.Balance == "2000000" && a.Account == "0xA76bFE6d90952Eb0a3b8fA31dF2580692acEf772")
                            || (a.Balance == "3000000" && a.Account == "0xA76bFE6d90952Eb0a3b8fA31dF2580692acEf772")
                            && a.EndBlock == 7035345
                            && b.Count == 2
                        )
                )),
            Times.Once());
    }

    [Fact]
    public async Task CalculateGenesisBalanceSnapshot_WhenGenesisValidatorNotExists()
    {
        var cycle = new RewardCycle
        {
            StartBlock = 7035346,
            EndBlock = 7035347
        };

        _genesisValidatorService.Setup(x => x.GetGenesisValidators()).ReturnsAsync(new List<GenesisValidatorModel>());

        await _balanceSnapshotResolver.CalculateGenesisBalanceSnapshot(cycle);

        _genesisValidatorService.Verify(x => x.GetGenesisValidators(), Times.Once);
        _balanceSnapshotService.Verify(x =>
                x.UpsertBalanceSnapshots(It.IsAny<List<BalanceSnapshotModel>>()),
            Times.Never());
    }

    [Fact]
    public async Task CalculateBalanceSnapshot_WhenBalanceSnapshotOfRewardCycleNotExisting()
    {
        //assume the following reward cycle's base balance dose not exist
        //which means the cycle (previous cycle) ends at 7035345 (7035346 - 1) does not have balance snapshot
        var cycle = new RewardCycle
        {
            StartBlock = 500001,
            EndBlock = 502001
        };
        var previousCycle = new RewardCycleModel()
        {
            StartBlock = 407001,
            EndBlock = cycle.StartBlock - 1
        };
        _rewardCycleService.Setup(x => x.GetRewardCycleByEndBlock(previousCycle.EndBlock)).ReturnsAsync(previousCycle);
        _balanceSnapshotService.Setup(x => x.HasCycleHaveBaseBalance(previousCycle.StartBlock)).ReturnsAsync(true);

        var previousCycleBalanceSnapshot = new List<BalanceSnapshotModel>()
        {
            new()
            {
                Account = "0xcb1de4FADCA68F601871f7E6E47fd43D707c779A",
                Balance = "1000000",
                EndBlock = previousCycle.StartBlock - 1
            },
            new()
            {
                Account = "0xc7ce0031385e4aD4e63F978872C911096B603455",
                Balance = "2000000",
                EndBlock = previousCycle.StartBlock - 1
            },
            new()
            {
                Account = "0x526576F7DF04894f1cA3f11c9C1e7d885d87B73f",
                Balance = "4000000",
                EndBlock = previousCycle.StartBlock - 1
            }
        };

        _balanceSnapshotService.Setup(x =>
                x.GetBalanceSnapshotByCycleEndBlock(previousCycle.StartBlock - 1))
            .ReturnsAsync(previousCycleBalanceSnapshot);

        var lastBalanceChanges = new List<BalanceModel>()
        {
            new()
            {
                Account = "0xcb1de4FADCA68F601871f7E6E47fd43D707c779A",
                BalanceChange = "1000000",
                EndBlock = previousCycle.StartBlock + 100
            },
            new()
            {
                Account = "0xc7ce0031385e4aD4e63F978872C911096B603455",
                BalanceChange = "1000000",
                EndBlock = previousCycle.StartBlock + 100
            }
        };

        _balanceChangeService.Setup(x =>
                x.GetUsersLastBalanceChanges(previousCycle.StartBlock, previousCycle.EndBlock))
            .ReturnsAsync(lastBalanceChanges);

        await _balanceSnapshotResolver.CalculateBalanceSnapshot(cycle);

        _rewardCycleService.Verify(x => x.GetRewardCycleByEndBlock(previousCycle.EndBlock), Times.Once);
        _balanceSnapshotService.Verify(x => x.HasCycleHaveBaseBalance(previousCycle.StartBlock), Times.Once);
        _balanceSnapshotService.Verify(x => x.GetBalanceSnapshotByCycleEndBlock(previousCycle.StartBlock - 1),
            Times.Once);
        _balanceChangeService.Verify(
            x => x.GetUsersLastBalanceChanges(previousCycle.StartBlock, previousCycle.EndBlock), Times.Once);
        _balanceSnapshotService.Verify(x => 
                x.UpsertBalanceSnapshots(It.Is<List<BalanceSnapshotModel>>(
                    b =>
                        b.Exists(a =>
                            (a.Balance == "1000000" && a.Account == "0xcb1de4FADCA68F601871f7E6E47fd43D707c779A")
                            || (a.Balance == "3000000" && a.Account == "0xc7ce0031385e4aD4e63F978872C911096B603455")
                            || (a.Balance == "4000000" && a.Account == "0x526576F7DF04894f1cA3f11c9C1e7d885d87B73f")
                            && a.EndBlock == 500000
                            && b.Count == 2
                        )
                    )),
            Times.Once);
    }
    
    [Fact]
    public async Task CalculateBalanceSnapshot_WhenPreviousCycleBaseBalanceNotExisting_ThrowExeception()
    {
        var cycle = new RewardCycle
        {
            StartBlock = 500001,
            EndBlock = 502001
        };
        var previousCycle = new RewardCycleModel
        {
            StartBlock = 407001,
            EndBlock = cycle.StartBlock - 1
        };
        _rewardCycleService.Setup(x => x.GetRewardCycleByEndBlock(previousCycle.EndBlock)).ReturnsAsync(previousCycle);
        _balanceSnapshotService.Setup(x => x.HasCycleHaveBaseBalance(previousCycle.StartBlock)).ReturnsAsync(false);

        await Assert.ThrowsAsync<BalanceSnapshotException>(() => _balanceSnapshotResolver.CalculateBalanceSnapshot(cycle));

        _rewardCycleService.Verify(x => x.GetRewardCycleByEndBlock(previousCycle.EndBlock), Times.Once);
        _balanceSnapshotService.Verify(x => x.HasCycleHaveBaseBalance(previousCycle.StartBlock), Times.Once);
        _balanceSnapshotService.Verify(x => x.GetBalanceSnapshotByCycleEndBlock(previousCycle.StartBlock - 1),
            Times.Never);
        _balanceChangeService.Verify(
            x => x.GetUsersLastBalanceChanges(previousCycle.StartBlock, previousCycle.EndBlock), Times.Never);
        _balanceSnapshotService.Verify(x => 
                x.UpsertBalanceSnapshots(It.IsAny<List<BalanceSnapshotModel>>()),
            Times.Never);
    }
}