using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Exceptions;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.Resolvers;

public interface IRewardCycleResolver
{
    /// <summary>
    /// Get reward cycles that are not calculated yet
    /// Assume the ear events are already fetched
    /// </summary>
    /// <returns>RewardCycles that need to calculate</returns>
    Task<List<RewardCycle>?> GetToBeCalculatedCycles();

    /// <summary>
    /// Get Era Index Range by block range
    /// </summary>
    /// <param name="startBlock"></param>
    /// <param name="endBlock"></param>
    /// <returns></returns>
    Task<Tuple<int, int>> GetRewardCycleEraIndexRangeByBlockRange(int startBlock, int endBlock);
}

/// <summary>
/// Reward Cycle Resolver
/// It is responsible for calculating reward cycles
/// 
/// </summary>
public class RewardCycleResolver : IRewardCycleResolver
{
    private readonly IEraService _eraService;
    private readonly IRewardCycleService _rewardCycleService;
    private ILogger<RewardCycleResolver> _logger;

    public RewardCycleResolver(IEraService eraService, IRewardCycleService rewardCycleService,
        ILogger<RewardCycleResolver> logger)
    {
        _logger = logger;
        _rewardCycleService = rewardCycleService;
        _eraService = eraService;
    }

    /// <summary>
    /// Get reward cycles that are not calculated yet
    /// Assume the ear events are already fetched
    /// </summary>
    /// <returns>RewardCycles that need to calculate</returns>
    public async Task<List<RewardCycle>?> GetToBeCalculatedCycles()
    {
        try
        {
            // get latest finished era
            var latestFinishedEra = await _eraService.GetLatestFinishedEra();
            _logger.LogInformation($"Latest finished era: {latestFinishedEra.EraIndex}");
            // get latest finished era index
            var latestFinishedEraIndex = latestFinishedEra.EraIndex;
            // get current reward cycle
            var currentRewardCycle = await _rewardCycleService.GetCurrentRewardCycle();
            _logger.LogInformation(
                $"Current reward cycle start era {currentRewardCycle.StartEraIndex} - start block {currentRewardCycle.StartBlock} ");

            var currentCycleStartEraIndex = currentRewardCycle.StartEraIndex;
            var cycleNumbers = CalculateCycleNumbers(latestFinishedEraIndex, currentCycleStartEraIndex);

            _logger.LogInformation($"{cycleNumbers} cycles need to be calculated");

            var rewardCycles =
                await CalculateNotFinishedRewardCycles(currentCycleStartEraIndex, latestFinishedEraIndex, cycleNumbers);

            _logger.LogInformation($"Reward cycles to be calculated: {rewardCycles}");
            // calculate and return reward cycles
            // return reward cycles that consist of
            //     startEraIndex, endEraIndex, startBlock, endBlock 
            return rewardCycles;
        }
        catch (EraException ee)
        {
            return null;
        }
        catch (RewardCycleException re)
        {
            return null;
        }
    }

    public async Task<Tuple<int, int>> GetRewardCycleEraIndexRangeByBlockRange(int startBlock, int endBlock)
    {
        var eraList = await _eraService.GetEraListByBlockRange(startBlock, endBlock);
        var startEraIndex = eraList.First().EraIndex;
        var endEraIndex = eraList.Last().EraIndex;
        return new Tuple<int, int>(startEraIndex, endEraIndex);
    }

    public async Task<List<RewardCycle>?> CalculateNotFinishedRewardCycles(int currentCycleStartEraIndex,
        int latestFinishedEraIndex,
        int cycleNumbers)
    {
        var rewardCycles = new List<RewardCycle>();
        // calculate how many reward cycles need to be calculated
        var startEraIndex = currentCycleStartEraIndex;
        for (var i = 0; i < cycleNumbers; i++)
        {
            // if assuming 2 rounds, threshold 90 eras
            //   starting from era 350, current reward cycle era index is 438, but the latest finished era is 441.
            //   which means that it needs to calculate from 350 to 439 for 90 eras in first round
            //   then it needs to complete first round cycle
            //      and move to calculate second round cycle from era 440 to 441 
            // if assuming 1 round, threshold 90 eras
            //   start from era 350, current reward cycle era index is 438, and the latest finished era is 439.
            //   it means that it needs to calculate from 350 to 439 for 90 eras in first round.
            //   current reward cycle only needs to be completed in next run.
            startEraIndex += i * RewardCircleConstants.RewardCircleThreshold;
            // first round 350 + 90 - 1 = 439, second round 440 + 90 - 1 = 529
            // so it needs to check if currentActiveEra.eraIndex <= endEraIndex
            var endEraIndex = startEraIndex + RewardCircleConstants.RewardCircleThreshold - 1;
            // calculate reward cycles that need to be calcualted
            // return array of reward cycles that need to be calculated
            // e.g. [{startEraIndex: 350, endEraIndex: 439}, {startEraIndex: 440, endEraIndex: 529}]
            if (latestFinishedEraIndex <= endEraIndex)
            {
                endEraIndex = latestFinishedEraIndex;
            }

            var startEra = await _eraService.GetEraByIndex(startEraIndex);
            var endEra = await _eraService.GetEraByIndex(endEraIndex);
            var startBlock = startEra.StartBlock;
            var endBlock = endEra.EndBlock;
            rewardCycles.Add(new RewardCycle
            {
                StartEraIndex = startEraIndex,
                EndEraIndex = endEraIndex,
                StartBlock = startBlock,
                EndBlock = endBlock
            });
        }

        return rewardCycles;
    }

    public static int CalculateCycleNumbers(int latestFinishedEraIndex, int currentCycleStartEraIndex)
    {
        var exceedOneCycle =
            latestFinishedEraIndex - currentCycleStartEraIndex + 1 >
            RewardCircleConstants.RewardCircleThreshold;
        var cycleNumbers = exceedOneCycle
            ? (int)Math.Ceiling(
                (latestFinishedEraIndex - currentCycleStartEraIndex + 1)
                / new decimal(RewardCircleConstants.RewardCircleThreshold))
            : 1;
        return cycleNumbers;
    }

  
    
}