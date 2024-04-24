using MatrixEngine.Core.Engine;
using MatrixEngine.Core.Exceptions;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatrixEngine.Core.Testing.Resolvers;

public class RewardCycleResolverTests
{
    private Mock<IEraService> _eraServiceMock;
    private Mock<IRewardCycleService> _rewardCycleServiceMock;
    private Mock<ILogger<RewardCycleResolver>> _logger;
    private RewardCycleResolver _resolver;

    public RewardCycleResolverTests()
    {
        _eraServiceMock = new Mock<IEraService>();
        _rewardCycleServiceMock = new Mock<IRewardCycleService>();
        _logger = new Mock<ILogger<RewardCycleResolver>>();
        _resolver = new RewardCycleResolver(_eraServiceMock.Object, _rewardCycleServiceMock.Object, _logger.Object);
    }
    [Fact]
    public void CalculateCycleNumbers_WhenItIsInOneCycle()
    {
        var latestFinishedEraIndex = 430;
        // current cycle is 350 - 439
        var currentCycleStartEraIndex = 350;

        var result = RewardCycleResolver.CalculateCycleNumbers(
            latestFinishedEraIndex, currentCycleStartEraIndex);

        Assert.Equal(1, result);
    }

    [Fact]
    public void CalculateCycleNumbers_WhenItIsInTwoCycles()
    {
        var latestFinishedEraIndex = 441;
        // current cycle is 350 - 439
        var currentCycleStartEraIndex = 350;

        var result = RewardCycleResolver.CalculateCycleNumbers(
            latestFinishedEraIndex, currentCycleStartEraIndex);

        Assert.Equal(2, result);
    }

    [Fact]
    public async Task CalculateNotFinishedRewardCycles_WhenItIsInOneCycle()
    {
        var currentCycleStartEraIndex = 350;
        //also the end era index for calculation in current cycle 
        var latestFinishedEraIndex = 430;
        var cycleNumbers = 1;


        _eraServiceMock.Setup(m => m.GetEraByIndex(currentCycleStartEraIndex))
            .ReturnsAsync(new EraModel
            {
                EraIndex = currentCycleStartEraIndex,
                StartBlock = 8995037,
                EndBlock = 9016603
            });

        _eraServiceMock.Setup(m => m.GetEraByIndex(latestFinishedEraIndex))
            .ReturnsAsync(new EraModel
            {
                EraIndex = latestFinishedEraIndex,
                StartBlock = 10721095,
                EndBlock = 10742692,
            });

        _rewardCycleServiceMock.Setup(m => m.GetCurrentRewardCycle())
            .ReturnsAsync(new RewardCycleModel() { StartEraIndex = currentCycleStartEraIndex });

        var result = await _resolver.CalculateNotFinishedRewardCycles(
            currentCycleStartEraIndex, latestFinishedEraIndex, cycleNumbers);

        Assert.Single(result);
        Assert.Equal(8995037, result.FirstOrDefault()?.StartBlock);
        Assert.Equal(10742692, result.FirstOrDefault()?.EndBlock);
        Assert.Equal(350, result.FirstOrDefault()?.StartEraIndex);
        Assert.Equal(430, result.FirstOrDefault()?.EndEraIndex);
    }

    [Fact]
    public async Task CalculateNotFinishedRewardCycles_WhenItIsInTwoCycles()
    {
        var currentCycleStartEraIndex = 350;
        var firstCycleEndEraIndex = 439;
        //also the end era index for calculation in current cycle 
        var secondCycleStartEraIndex = 440;
        var latestFinishedEraIndex = 441;
        var cycleNumbers = 2;


        _eraServiceMock.Setup(m => m.GetEraByIndex(currentCycleStartEraIndex))
            .ReturnsAsync(new EraModel
            {
                EraIndex = currentCycleStartEraIndex,
                StartBlock = 8995037,
                EndBlock = 9016603
            });

        _eraServiceMock.Setup(m => m.GetEraByIndex(firstCycleEndEraIndex))
            .ReturnsAsync(new EraModel
            {
                EraIndex = firstCycleEndEraIndex,
                StartBlock = 10915472,
                EndBlock = 10937071
            });

        _eraServiceMock.Setup(m => m.GetEraByIndex(secondCycleStartEraIndex))
            .ReturnsAsync(new EraModel
            {
                EraIndex = secondCycleStartEraIndex,
                StartBlock = 10937072,
                EndBlock = 10958671
            });

        _eraServiceMock.Setup(m => m.GetEraByIndex(latestFinishedEraIndex))
            .ReturnsAsync(new EraModel
            {
                EraIndex = latestFinishedEraIndex,
                StartBlock = 10958672,
                EndBlock = 10978673,
            });

        _rewardCycleServiceMock.Setup(m => m.GetCurrentRewardCycle())
            .ReturnsAsync(new RewardCycleModel() { StartEraIndex = currentCycleStartEraIndex });

        var result = await _resolver.CalculateNotFinishedRewardCycles(
            currentCycleStartEraIndex, latestFinishedEraIndex, cycleNumbers);

        Assert.Equal(2, result.Count);

        Assert.Equal(8995037, result.FirstOrDefault()?.StartBlock);
        Assert.Equal(10937071, result.FirstOrDefault()?.EndBlock);
        Assert.Equal(350, result.FirstOrDefault()?.StartEraIndex);
        Assert.Equal(439, result.FirstOrDefault()?.EndEraIndex);

        Assert.Equal(10937072, result.LastOrDefault()?.StartBlock);
        Assert.Equal(10978673, result.LastOrDefault()?.EndBlock);
        Assert.Equal(440, result.LastOrDefault()?.StartEraIndex);
        Assert.Equal(441, result.LastOrDefault()?.EndEraIndex);
    }

    [Fact]
    public async Task GetToBeCalculatedCycles_WhenOnlyOneCycle()
    {
        var currentCycleStartEraIndex = 350;
        //also the end era index for calculation in current cycle 
        var latestFinishedEraIndex = 430;


        _eraServiceMock.Setup(m => m.GetLatestFinishedEra())
            .ReturnsAsync(
                new EraModel
                {
                    EraIndex = latestFinishedEraIndex,
                    StartBlock = 10721095,
                    EndBlock = 10742692,
                }
            );
        _eraServiceMock.Setup(m => m.GetEraByIndex(currentCycleStartEraIndex))
            .ReturnsAsync(new EraModel
            {
                EraIndex = currentCycleStartEraIndex,
                StartBlock = 8995037,
                EndBlock = 9016603
            });

        _eraServiceMock.Setup(m => m.GetEraByIndex(latestFinishedEraIndex))
            .ReturnsAsync(new EraModel
            {
                EraIndex = latestFinishedEraIndex,
                StartBlock = 10721095,
                EndBlock = 10742692,
            });

        _rewardCycleServiceMock.Setup(m => m.GetCurrentRewardCycle())
            .ReturnsAsync(new RewardCycleModel() { StartEraIndex = currentCycleStartEraIndex });

        var result = await _resolver.GetToBeCalculatedCycles();

        Assert.Single(result);
        Assert.Equal(8995037, result.FirstOrDefault()?.StartBlock);
        Assert.Equal(10742692, result.FirstOrDefault()?.EndBlock);
        Assert.Equal(350, result.FirstOrDefault()?.StartEraIndex);
        Assert.Equal(430, result.FirstOrDefault()?.EndEraIndex);
    }

    [Fact]
    public async Task GetToBeCalculatedCycles_WhenTwoCyclesNeedToCalculate()
    {
        
        var currentCycleStartEraIndex = 350;
        var firstCycleEndEraIndex = 439;
        //also the end era index for calculation in current cycle 
        var secondCycleStartEraIndex = 440;
        var latestFinishedEraIndex = 441;
        var cycleNumbers = 2;


        _eraServiceMock.Setup(m => m.GetLatestFinishedEra())
            .ReturnsAsync(
                new EraModel
                {
                    EraIndex = latestFinishedEraIndex,
                    StartBlock = 10958672,
                    EndBlock = 10978673,
                }
            );
        _eraServiceMock.Setup(m => m.GetEraByIndex(currentCycleStartEraIndex))
            .ReturnsAsync(new EraModel
            {
                EraIndex = currentCycleStartEraIndex,
                StartBlock = 8995037,
                EndBlock = 9016603
            });

        _eraServiceMock.Setup(m => m.GetEraByIndex(firstCycleEndEraIndex))
            .ReturnsAsync(new EraModel
            {
                EraIndex = firstCycleEndEraIndex,
                StartBlock = 10915472,
                EndBlock = 10937071
            });

        _eraServiceMock.Setup(m => m.GetEraByIndex(secondCycleStartEraIndex))
            .ReturnsAsync(new EraModel
            {
                EraIndex = secondCycleStartEraIndex,
                StartBlock = 10937072,
                EndBlock = 10958671
            });

        _eraServiceMock.Setup(m => m.GetEraByIndex(latestFinishedEraIndex))
            .ReturnsAsync(new EraModel
            {
                EraIndex = latestFinishedEraIndex,
                StartBlock = 10958672,
                EndBlock = 10978673,
            });

        _rewardCycleServiceMock.Setup(m => m.GetCurrentRewardCycle())
            .ReturnsAsync(new RewardCycleModel() { StartEraIndex = currentCycleStartEraIndex });

        var result = await _resolver.GetToBeCalculatedCycles();

        Assert.Equal(2, result.Count);

        Assert.Equal(8995037, result.FirstOrDefault()?.StartBlock);
        Assert.Equal(10937071, result.FirstOrDefault()?.EndBlock);
        Assert.Equal(350, result.FirstOrDefault()?.StartEraIndex);
        Assert.Equal(439, result.FirstOrDefault()?.EndEraIndex);

        Assert.Equal(10937072, result.LastOrDefault()?.StartBlock);
        Assert.Equal(10978673, result.LastOrDefault()?.EndBlock);
        Assert.Equal(440, result.LastOrDefault()?.StartEraIndex);
        Assert.Equal(441, result.LastOrDefault()?.EndEraIndex);
    }
    [Fact]
    public void GetToBeCalculatedCycles_WhenNoLastFinishedEra()
    {
        _eraServiceMock.Setup(m => m.GetLatestFinishedEra())
            .Throws<EraException>();

        var result = _resolver.GetToBeCalculatedCycles().Result;

        Assert.Null(result);
    }
    
    [Fact]
    public void GetToBeCalculatedCycles_WhenNoCurrentRewardCycle()
    {
        _eraServiceMock.Setup(m => m.GetLatestFinishedEra())
            .ReturnsAsync(new EraModel { EraIndex = 430 });


        _rewardCycleServiceMock.Setup(m => m.GetCurrentRewardCycle())
            .Throws<RewardCycleException>();

        var result = _resolver.GetToBeCalculatedCycles().Result;

        Assert.Null(result);
    }
}