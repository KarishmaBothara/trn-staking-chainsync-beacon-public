using MatrixEngine.Core.Testing.Fixtures;
using MatrixEngine.Core.Exceptions;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Extensions.Ordering;

namespace MatrixEngine.Core.Testing.Services;

[CollectionDefinition("Database Collection"), Order(4)]
public class RewardCycleServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly RewardCycleService _rewardCycleService;
    
    public RewardCycleServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        var loggerMock = new Mock<ILogger<RewardCycleService>>();
        _rewardCycleService = new RewardCycleService(_fixture.Database, loggerMock.Object);
    }

    [Fact]
    public async Task GetCurrentRewardCycle_WhenNoActiveCycleExists_ThrowsException()
    {
        // Arrange
        _fixture.ClearRewardCycleData(); 

        // Act
        async Task Act() => await _rewardCycleService.GetCurrentRewardCycle();

        // Assert
        await Assert.ThrowsAsync<RewardCycleException>(Act);
    }
    
    [Fact]
    public async Task GetCurrentRewardCycle_WhenActiveCycleExists_ReturnsCycle()
    {
        // Arrange
        
        _fixture.BuildRewardCycleData();

        // Act
        var rewardCycle = await _rewardCycleService.GetCurrentRewardCycle();

        // Assert
        Assert.NotNull(rewardCycle);
    }
}