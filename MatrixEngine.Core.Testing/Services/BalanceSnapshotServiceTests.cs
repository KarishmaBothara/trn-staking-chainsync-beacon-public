using MatrixEngine.Core.Models;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Testing.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Extensions.Ordering;

namespace MatrixEngine.Core.Testing.Services;

[CollectionDefinition("Database Collection"), Order(6)]
public class BalanceSnapshotServiceTests: IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly Mock<ILogger<BalanceSnapshotService>> _logger;
    private readonly BalanceSnapshotService _balanceSnapshotService;

    public BalanceSnapshotServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _logger = new Mock<ILogger<BalanceSnapshotService>>();
        _balanceSnapshotService = new BalanceSnapshotService(_fixture.Database, _logger.Object);
    }

    [Fact]
    public async Task GetBalanceSnapshotByCycleEndBlock_WhenBalanceSnapshotExisting()
    {
        _fixture.BuildBalanceSnapshotData();
        const int endBlock = 7035346;
        
        var balanceSnapshots = await _balanceSnapshotService.GetBalanceSnapshotByCycleEndBlock(endBlock);
        Assert.Equal(3, balanceSnapshots.Count);
    }
    
    [Fact]
    public async Task GetBalanceSnapshotByCycleEndBlock_WhenBalanceSnapshotNotExisting()
    {
        _fixture.BuildBalanceSnapshotData();
        const int endBlock = 7035347;
        
        var balanceSnapshots = await _balanceSnapshotService.GetBalanceSnapshotByCycleEndBlock(endBlock);
        Assert.Empty(balanceSnapshots);
    }
   
    [Fact]
    public async Task HasCycleHaveBaseBalance_WhenBaseBalanceExisting()
    {
        _fixture.BuildBalanceSnapshotData();
        const int startBlock = 7035347;
        
        var hasBaseBalance = await _balanceSnapshotService.HasCycleHaveBaseBalance(startBlock);
        Assert.True(hasBaseBalance);
    }
    
    [Fact]
    public async Task HasCycleHaveBaseBalance_WhenBaseBalanceNotExisting()
    {
        _fixture.BuildBalanceSnapshotData();
        const int startBlock = 7035346;
        
        var hasBaseBalance = await _balanceSnapshotService.HasCycleHaveBaseBalance(startBlock);
        Assert.False(hasBaseBalance);
    }
    
    [Fact]
    public async Task UpsertBalanceSnapshots_WhenBalanceSnapshotsAreNew()
    {
        _fixture.BuildBalanceSnapshotData();
        var balanceSnapshots = new List<BalanceSnapshotModel>
        {
            new BalanceSnapshotModel
            {
                EndBlock = 8035347,
                Account = "0xA76bFE6d90952Eb0a3b8fA31dF2580692acEf772",
                Balance = "3000000" 
            }
        };
        
        await _balanceSnapshotService.UpsertBalanceSnapshots(balanceSnapshots);
        var balanceSnapshotsFromDb = await _balanceSnapshotService.GetBalanceSnapshotByCycleEndBlock(8035347);
        Assert.Single(balanceSnapshotsFromDb);
        Assert.Equal(8035347, balanceSnapshotsFromDb[0].EndBlock);
        Assert.Equal("0xA76bFE6d90952Eb0a3b8fA31dF2580692acEf772", balanceSnapshotsFromDb[0].Account);
        Assert.Equal("3000000", balanceSnapshotsFromDb[0].Balance);
    }
}