using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Exceptions;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.DTOs;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Substrate;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatrixEngine.Core.Resolvers;

public interface IRewardCycleResolver
{
    /// <summary>
    /// Get reward cycles that are not calculated yet
    /// Assume the era events are already fetched
    /// </summary>
    /// <returns>RewardCycles that need to calculate</returns>
    Task<List<RewardCycle>?> GetToBeCalculatedCycles(int latestFinishedEraIndex);

    /// <summary>
    /// Get Era Index Range by block range
    /// </summary>
    /// <param name="startBlock"></param>
    /// <param name="endBlock"></param>
    /// <returns></returns>
    Task<Tuple<int, int>> GetRewardCycleEraIndexRangeByBlockRange(int startBlock, int endBlock);

    Task CheckRewardCycle(RewardCycle rewardCycle);

    Task UpdateRewardCycleCurrentEra(RewardCycle rewardCycle, int currentEra);

    Task CreateNewRewardCycle(RewardCycle cycle);
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
    private readonly ILogger<RewardCycleResolver> _logger;
    private readonly string _network;

    public RewardCycleResolver(
        IOptions<SubstrateSettings> options,
        IEraService eraService,
        IRewardCycleService rewardCycleService,
        ILogger<RewardCycleResolver> logger)
    {
        _logger = logger;
        _rewardCycleService = rewardCycleService;
        _eraService = eraService;
        _network = options.Value.Network;
    }

    /// <summary>
    /// Get reward cycles that are not calculated yet
    /// Assume the era events are already fetched
    /// </summary>
    /// <returns>RewardCycles that need to calculate</returns>
    public async Task<List<RewardCycle>?> GetToBeCalculatedCycles(int latestFinishedEraIndex)
    {
        try
        {
            // get current reward cycle
            var currentRewardCycle = await GetCurrentRewardCycle();
            if (currentRewardCycle == null)
            {
                return null;
            }

            var rewardCycles = new List<RewardCycle>();
            // If this is the first distribution and it doesn't start at genesis, we need to create a fake distribution cycle
            // for the first part of it's life to calculate all the effective balances up until this point.
            if (currentRewardCycle.VtxDistributionId == 0 && currentRewardCycle.StartBlock > 0)
            {
                var genesisCycle = new RewardCycle
                {
                    StartBlock = 0,
                    EndBlock = currentRewardCycle.StartBlock - 1,
                    StartEraIndex = 0,
                    EndEraIndex = currentRewardCycle.StartEraIndex - 1,
                    VtxDistributionId = -1 // Set ID to Negative to indicate it's a fake cycle
                };
                // Need to save the genesis cycle to the DB so we can update it later
                await CreateNewRewardCycle(genesisCycle);
                rewardCycles.Add(genesisCycle);
            }

            var currentCycleStartEraIndex = currentRewardCycle.StartEraIndex;
            var notFinishedRewardCycles = await CalculateNotFinishedRewardCycles(
                latestFinishedEraIndex,
                currentRewardCycle.StartEraIndex,
                currentRewardCycle.VtxDistributionId
            );
            if (notFinishedRewardCycles != null)
            {
                rewardCycles.AddRange(notFinishedRewardCycles);
            }
            _logger.LogInformation($"{rewardCycles.Count} cycles need to be calculated");
            return rewardCycles;
        }
        catch (EraException ee)
        {
            _logger.LogError(ee?.Message);
            return null;
        }
        catch (RewardCycleException re)
        {
            _logger.LogError(re?.Message);
            return null;
        }
    }

    // Get the current reward cycle.
    // If there is none, create the first one depending on if we are on Porcini or Mainnet
    private async Task<RewardCycleModel?> GetCurrentRewardCycle()
    {
        var rewardCycle = await _rewardCycleService.GetCurrentRewardCycle();
        if (rewardCycle == null)
        {
            var startBlock = -1;
            var startEraIndex = -1;
            if (_network == "mainnet" || _network == "root")
            {
                // This is the correct values for MAINNET based on previous reward cycle
                startBlock = 7035347;
                startEraIndex = 259;
            }
            else if (_network == "porcini")
            {
                // This is the correct values for PORCINI based on previous reward cycle
                // These values are used on our current Porcini test
                startBlock = 5402974;
                startEraIndex = 259;
            }

            // If we are on Porcini or Mainnet and there is no reward cycle, create the first one
            if (startBlock != -1)
            {
                await CreateNewRewardCycle(new()
                {
                    VtxDistributionId = 0,
                    StartBlock = startBlock,
                    StartEraIndex = startEraIndex,
                });
                rewardCycle = await _rewardCycleService.GetCurrentRewardCycle();
                _logger.LogInformation($"==> Creating first reward cycle for {_network} start era {startEraIndex} - start block {startBlock} ");
            }
        }

        // There is still a chance that reward cycle is null after creating the first entry
        if (rewardCycle == null)
        {
            _logger.LogError("No active reward cycle found");
            return null;
        }

        _logger.LogInformation(
            $"==> Current reward cycle start era {rewardCycle.StartEraIndex} - start block {rewardCycle.StartBlock} ");
        return rewardCycle;
    }

    public async Task<Tuple<int, int>> GetRewardCycleEraIndexRangeByBlockRange(int startBlock, int endBlock)
    {
        var eraList = await _eraService.GetEraListByBlockRange(startBlock, endBlock);
        var startEraIndex = eraList.First().EraIndex;
        var endEraIndex = eraList.Last().EraIndex;
        return new Tuple<int, int>(startEraIndex, endEraIndex);
    }

    public async Task<List<RewardCycle>?> CalculateNotFinishedRewardCycles(
        int latestFinishedEraIndex,
        int currentCycleStartEraIndex,
        int currentVtxDistId
    )
    {
        var rewardCycles = new List<RewardCycle>();
        // calculate how many reward cycles need to be calculated
        var startEraIndex = currentCycleStartEraIndex;
        while (startEraIndex <= latestFinishedEraIndex)
        {
            // Add the threshold (90) - 1 onto the startEraIndex. startEraIndex is updated each iteration
            var endEraIndex = startEraIndex + RewardCycleConstants.RewardCycleThreshold - 1;

            // Special cases
            if (startEraIndex == 709)
            {
                // We had a mis calculation on era 709 where the end block is actually 799
                endEraIndex += 1;
            }
            else if (startEraIndex == 890 && _network == "porcini")
            {
                // On Porcini we want to test reward cycle at 955 for the current cycle. For this reason we are cutting the
                // threshold short
                endEraIndex = 955;
            } else if (startEraIndex == 980) {
                // On mainnet we missed two reward cycles, so combining these to 180 days cycle
                endEraIndex = 1159;
            }

            // If we have not yet completed the reward cycle, set the endBlock to -1
            // We should still return the cycle so we can calculate the balance change ledger per cycle
            var endBlock = -1;
            if (latestFinishedEraIndex >= endEraIndex)
            {
                var endEra = await _eraService.GetEraByIndex(endEraIndex);
                endBlock = endEra.EndBlock;
            }
            var startEra = await _eraService.GetEraByIndex(startEraIndex);
            rewardCycles.Add(new RewardCycle
            {
                StartEraIndex = startEraIndex,
                EndEraIndex = endEraIndex,
                StartBlock = startEra.StartBlock,
                EndBlock = endBlock,
                VtxDistributionId = currentVtxDistId
            });

            // Moved to end of function to add constant amount per iteration.
            startEraIndex = endEraIndex + 1;
            currentVtxDistId += 1;
        }

        return rewardCycles;
    }

    // At the end of a calculations run, check whether the reward cycle is complete and update it if needed
    public async Task CheckRewardCycle(RewardCycle rewardCycle)
    {
        // get latest finished era
        var latestFinishedEra = await _eraService.GetLatestFinishedEra();
        if (latestFinishedEra == null) throw new Exception("Can't find latest finished era");

        _logger.LogInformation($"Latest finished era: {latestFinishedEra.EraIndex}");
        // get latest finished era index
        var latestFinishedEraIndex = latestFinishedEra.EraIndex;

        var cycleLength = RewardCycleConstants.RewardCycleThreshold;
        if (rewardCycle.StartEraIndex == 709)
        {
            cycleLength = 91;
        }
        else if (rewardCycle.StartEraIndex == 890 && _network == "porcini")
        {
            cycleLength = 66;
        } else if (rewardCycle.StartEraIndex == 980) {
            cycleLength = 180;
        }

        // check if the reward cycle is complete
        // This is checked by comparing the supposed end with the 90 era threshold
        if (latestFinishedEraIndex - rewardCycle.StartEraIndex + 1 >= cycleLength)
        {
            // Current Reward Cycle Finished: need to complete the reward cycle (db)
            _logger.LogInformation($"The cycle of ID: {rewardCycle.VtxDistributionId} " +
                                   $"for blocks {rewardCycle.StartBlock} - {rewardCycle.EndBlock} " +
                                   $"is finished and needs to complete");

            await CompleteRewardCycle(rewardCycle);
            // create next reward cycle (db)
            await CreateNextRewardCycle(rewardCycle);
        }
        else
        {
            // Current Reward Cycle Ongoing: set current era index to the current reward cycle
            // update current reward cycle (db)
            _logger.LogInformation($"The cycle has not finished yet, going to set {latestFinishedEraIndex} to current era index");
            await _rewardCycleService.UpdateCurrentEraIndexOfRewardCycle(rewardCycle.VtxDistributionId, latestFinishedEraIndex);
        }

        _logger.LogInformation($"Finished cycle (Era: {rewardCycle.StartEraIndex} - ) calculation");
    }

    // Used to insert fake reward cycles for easy testing
    public async Task CreateNewRewardCycle(RewardCycle cycle)
    {
        var newRewardCycle = new RewardCycleModel()
        {
            StartBlock = cycle.StartBlock,
            EndBlock = cycle.EndBlock,
            StartEraIndex = cycle.StartEraIndex,
            CurrentEraIndex = cycle.StartEraIndex,
            EndEraIndex = cycle.EndEraIndex,
            VtxDistributionId = cycle.VtxDistributionId,
            CalculationComplete = false
        };
        _logger.LogInformation(
            $"Create new reward cycle (block:{newRewardCycle.StartBlock}-{newRewardCycle.EndBlock}) (era start index: {newRewardCycle.StartEraIndex})");
        await _rewardCycleService.CreateRewardCycle(newRewardCycle);
    }

    // Create the next reward cycle and load it into the database.
    // Even if this cycle is not finished, we will be updating it every era with the currentEraIndex so need to create it.
    // Pass in the previous reward cycle to get the start block and era index for the next one
    private async Task CreateNextRewardCycle(RewardCycle prevRewardCycle)
    {
        var newRewardCycle = new RewardCycleModel()
        {
            VtxDistributionId = prevRewardCycle.VtxDistributionId + 1,
            StartBlock = prevRewardCycle.EndBlock + 1,
            StartEraIndex = prevRewardCycle.EndEraIndex + 1,
            CurrentEraIndex = prevRewardCycle.EndEraIndex + 1,
            EndEraIndex = -1,
            EndBlock = -1,
            CalculationComplete = false
        };
        _logger.LogInformation(
            $"Create new reward cycle (block:{newRewardCycle.StartBlock}-{newRewardCycle.EndBlock}) (era start index: {newRewardCycle.StartEraIndex})");
        await _rewardCycleService.CreateRewardCycle(newRewardCycle);
    }

    // Calls the service to update the stored model by setting the finished and need to calculate flags to true
    // Also set the official end block, based on the last era in the cycle
    private async Task CompleteRewardCycle(RewardCycle rewardCycle)
    {
        _logger.LogInformation($"Complete reward cycle ID: {rewardCycle.VtxDistributionId} (era: {rewardCycle.StartEraIndex} - {rewardCycle.EndEraIndex})");
        var lastEra = await _eraService.GetEraByIndex(rewardCycle.EndEraIndex);
        await _rewardCycleService.UpdateRewardCycleToComplete(rewardCycle, lastEra.EndBlock);
    }

    // Updates the current era of a reward cycle
    public async Task UpdateRewardCycleCurrentEra(RewardCycle rewardCycle, int currentEra)
    {
        await _rewardCycleService.UpdateRewardCycleCurrentEra(rewardCycle, currentEra);
    }
}
