using MatrixEngine.Core.Models;
using MatrixEngine.Core.Resolvers;
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
    private readonly ILogger<ComputingCore> _logger;
    private IBalanceSnapshotResolver _balanceSnapshotResolver;

    public ComputingCore(IRewardCycleResolver rewardCycleResolver, IBalanceSnapshotResolver balanceSnapshotResolver,
        IBalanceChangeResolver balanceChangeResolver, IEffectiveBalanceResolver effectiveBalanceResolver,
        ILogger<ComputingCore> logger)
    {
        _balanceSnapshotResolver = balanceSnapshotResolver;
        _effectiveBalanceResolver = effectiveBalanceResolver;
        _balanceChangeResolver = balanceChangeResolver;
        _rewardCycleResolver = rewardCycleResolver;
        _logger = logger;
    }

    public async Task Compute()
    {
        _logger.LogTrace("ComputingCore started.");
        // Should consider if current era has ended and new era has started
        // Which means more than one reward cycle data should be calculated
        var rewardCycles = await _rewardCycleResolver.GetToBeCalculatedCycles();

        if (rewardCycles == null || rewardCycles.Count == 0)
        {
            _logger.LogTrace("No reward cycle to calculate.");
            return;
        }

        _logger.LogTrace($"Calculating {rewardCycles.Count} cycles' effective balances.");
        foreach (var rewardCycle in rewardCycles)
        {
            await CalculateOneCycleEffectiveBalance(rewardCycle);
        }
    }
    
    private async Task CalculateOneCycleEffectiveBalance(RewardCycle rewardCycle)
    {
        _logger.LogInformation(
            $"Start calculating effective balances for cycle - Era: {rewardCycle.EndEraIndex} StartBlock: {rewardCycle.StartBlock}.");

        //TODO: check if the cycle has the balance snapshot, if not do the calculation 
        await _balanceSnapshotResolver.CalculateBalanceSnapshotInACycle(rewardCycle); 
        // load balance snapshot or load genesis validators' balance if it is the first cycle 
        // load balance snapshot use the one block ahead of start block in the cycle
        var allUsersBalanceChanges = await _balanceChangeResolver.ResolveBalanceChange(
            rewardCycle.StartBlock,
            rewardCycle.EndBlock
        );
        _logger.LogInformation($"{allUsersBalanceChanges?.Keys?.Count ?? 0} users balance changes resolved.");

        var effectiveBalances = _effectiveBalanceResolver.CalculateEffectiveBalances(allUsersBalanceChanges);
        _logger.LogInformation($"{effectiveBalances?.Keys?.Count ?? 0} users effective balances calculated.");

        var effectiveBalancesList = effectiveBalances?.Values.SelectMany(x => x).ToList();
        _logger.LogInformation(
            $"{effectiveBalancesList?.Count ?? 0} effective balances calculated. Saving to database.");
        if (effectiveBalancesList != null)
            await _effectiveBalanceResolver.SaveEffectiveBalances(effectiveBalancesList);
            
    }
}