using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Testing.Fixtures;
using MatrixEngine.Core.Exceptions;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
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

    [Fact]
    public async Task UpdateRewardCycle_WhenCycleExists()
    {
        var rewardCycleModel = new RewardCycleModel()
        {
            StartBlock = 8973464,
            StartEraIndex = 349,
            CurrentEraIndex = 410,
            Finished = false,
        };

        var collection = _fixture.Database.GetCollection<RewardCycleModel>(DbCollectionName.RewardCycle);
        //create model
        await collection.InsertOneAsync(rewardCycleModel);

        var updateRewardCycleModel = new RewardCycleModel()
        {
            StartBlock = 8973464,
            StartEraIndex = 349,
            EndBlock = 10915471,
            EndEraIndex = 438,
            CurrentEraIndex = 438,
            Finished = true,
        };

        await _rewardCycleService.UpdateRewardCycle(updateRewardCycleModel);

        var updatedModel = await collection
            .Find(Builders<RewardCycleModel>.Filter.Eq(x => x.StartBlock, 8973464))
            .FirstOrDefaultAsync();

        Assert.Equal(8973464, updatedModel.StartBlock);
        Assert.Equal(10915471, updatedModel.EndBlock);
        Assert.Equal(438, updatedModel.EndEraIndex);
        Assert.Equal(438, updatedModel.CurrentEraIndex);
        Assert.True(updatedModel.Finished);
    }

    [Fact]
    public async Task CreateRewardCycle_WhenRewardCycleDoesNotExist()
    {
        var rewardCycleModel = new RewardCycleModel()
        {
            StartBlock = 8973464,
            StartEraIndex = 349,
            CurrentEraIndex = 410,
            Finished = false,
        };

        await _rewardCycleService.CreateRewardCycle(rewardCycleModel);
        
        var collection = _fixture.Database.GetCollection<RewardCycleModel>(DbCollectionName.RewardCycle); 
        var createdModel = await collection
            .Find(Builders<RewardCycleModel>.Filter.Eq(x => x.StartBlock, 8973464))
            .FirstOrDefaultAsync();
        
        Assert.Equal(rewardCycleModel.StartBlock, createdModel.StartBlock);
        Assert.Equal(rewardCycleModel.StartEraIndex, createdModel.StartEraIndex);
        Assert.Equal(rewardCycleModel.CurrentEraIndex, createdModel.CurrentEraIndex);
        Assert.Equal(rewardCycleModel.Finished, createdModel.Finished);
    }

    [Fact]
    public async Task UpdateCurrentEraIndexOfRewardCycle_WhenCycleExists()
    {
        var rewardCycleModel = new RewardCycleModel()
        {
            StartBlock = 8973464,
            StartEraIndex = 349,
            CurrentEraIndex = 409,
            Finished = false,
        };

        var collection = _fixture.Database.GetCollection<RewardCycleModel>(DbCollectionName.RewardCycle);
        await collection.InsertOneAsync(rewardCycleModel);

        var updateCurrentEraIndexModel = new RewardCycleModel()
        {
            StartBlock = 8973464,
            StartEraIndex = 349,
            CurrentEraIndex = 410,
            Finished = false,
        };
        await _rewardCycleService.UpdateCurrentEraIndexOfRewardCycle(updateCurrentEraIndexModel);
        
        var updatedModel = await collection
            .Find(Builders<RewardCycleModel>.Filter.Eq(x => x.StartBlock, 8973464))
            .FirstOrDefaultAsync();
        
        Assert.Equal(updateCurrentEraIndexModel.StartBlock, updatedModel.StartBlock);
        Assert.Equal(updateCurrentEraIndexModel.StartEraIndex, updatedModel.StartEraIndex);
        Assert.Equal(updateCurrentEraIndexModel.StartEraIndex, updatedModel.StartEraIndex);
        Assert.Equal(updateCurrentEraIndexModel.CurrentEraIndex, updatedModel.CurrentEraIndex);
        Assert.Equal(updateCurrentEraIndexModel.Finished, updatedModel.Finished);
    }
}