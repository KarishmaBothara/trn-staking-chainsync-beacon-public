using MatrixEngine.Core.Exceptions;
using MatrixEngine.Core.Models.DTOs;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.Engine;

public interface IComputingCore
{
    Task Compute();
}

public class ComputingCore : IComputingCore
{
    private readonly IRewardCycleResolver _rewardCycleResolver;
    private readonly IBalanceChangeResolver _balanceChangeResolver;
    private readonly IEffectiveBalanceResolver _effectiveBalanceResolver;
    private readonly IEraService _eraService;
    private readonly ILogger<ComputingCore> _logger;

    public ComputingCore(
        IRewardCycleResolver rewardCycleResolver,
        IBalanceChangeResolver balanceChangeResolver, 
        IEffectiveBalanceResolver effectiveBalanceResolver,
        IEraService eraService,
        ILogger<ComputingCore> logger)
    {
        _effectiveBalanceResolver = effectiveBalanceResolver;
        _balanceChangeResolver = balanceChangeResolver;
        _rewardCycleResolver = rewardCycleResolver;
        _eraService = eraService;
        _logger = logger;
    }

    public async Task Compute()
    {
        _logger.LogTrace("ComputingCore started.");
        var latestFinishedEra = await _eraService.GetLatestFinishedEra();
        if (latestFinishedEra == null) throw new Exception("Can't find latest finished era");
        _logger.LogInformation($"Latest finished era: {latestFinishedEra.EraIndex}");

        // Should consider if current era has ended and new era has started
        // Which means more than one reward cycle data should be calculated
        var rewardCycles = await _rewardCycleResolver.GetToBeCalculatedCycles(latestFinishedEra.EraIndex);
        _logger.LogInformation("Found {RewardCyclesCount} reward cycles to be calculated", rewardCycles.Count);
        if (rewardCycles == null || rewardCycles.Count == 0)
        {
            _logger.LogTrace("No reward cycle to calculate.");
            return;
        }

        foreach (var rewardCycle in rewardCycles)
        {
            if (rewardCycle.EndBlock == -1)
            {
                _logger.LogInformation($"\n\n=======================> Reward cycle {rewardCycle.VtxDistributionId} is not finished yet, calculating balance changes.");
                // Calculate balance changes from startBlock to the current era endblock
                await _balanceChangeResolver.ResolveBalanceChange(
                    rewardCycle.StartBlock,
                    latestFinishedEra.EndBlock
                );
                // Update reward cycle current era index
                await _rewardCycleResolver.UpdateRewardCycleCurrentEra(rewardCycle, latestFinishedEra.EraIndex);
            }
            else
            {
                _logger.LogInformation($"\n\n=======================> Calculating New Reward Cycle! ID: {rewardCycle.VtxDistributionId} Blocks: {rewardCycle.StartBlock} - {rewardCycle.EndBlock} \n");
                // Resolve balance changes for whole cycle
                await _balanceChangeResolver.ResolveBalanceChange(
                    rewardCycle.StartBlock,
                    rewardCycle.EndBlock
                );
                await CalculateOneCycleEffectiveBalance(rewardCycle);
                await _rewardCycleResolver.CheckRewardCycle(rewardCycle);
            }
        }
        
        // Update era table with latest processed era index
        await _eraService.SetBalancesProcessed(latestFinishedEra.EraIndex);
    }
    
    private async Task CalculateOneCycleEffectiveBalance(RewardCycle rewardCycle)
    {
        _logger.LogInformation(
            $"Start calculating effective balances for cycle - Era: {rewardCycle.EndEraIndex} StartBlock: {rewardCycle.StartBlock}. VtxDistId: {rewardCycle.VtxDistributionId}");

        try
        {
            // Get the stored balance changes from the DB. This does not calculate the balance changes, it just gets the data
            var allUsersBalanceChanges = await _balanceChangeResolver.GetBalanceChangesInRange(
                rewardCycle.StartBlock,
                rewardCycle.EndBlock
            );
            _logger.LogInformation($"{allUsersBalanceChanges?.Keys?.Count ?? 0} users balance changes resolved.");
            
            var effectiveBalances = await _effectiveBalanceResolver.CalculateEffectiveBalances(
                allUsersBalanceChanges, 
                rewardCycle
            );
            _logger.LogInformation($"{effectiveBalances?.Keys?.Count ?? 0} users effective balances calculated.");
            
            var effectiveBalancesList = effectiveBalances?.Values.SelectMany(x => x).ToList();
            _logger.LogInformation($"{effectiveBalancesList?.Count ?? 0} effective balances calculated. Saving to database.");
            if (effectiveBalancesList != null)
            {
                await _effectiveBalanceResolver.SaveEffectiveBalances(effectiveBalancesList);
            }
        }
        catch (BalanceSnapshotException bse)
        {
            _logger.LogError(bse.Message);
            _logger.LogError(
                $"Stop calculating effective balance for cycle {rewardCycle.StartBlock} - {rewardCycle.EndBlock}");
        }
    }
}