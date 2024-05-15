using MatrixEngine.Core.Exceptions;
using MatrixEngine.Core.Models.DTOs;
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
    private readonly IBalanceSnapshotResolver _balanceSnapshotResolver;
    private readonly ILogger<ComputingCore> _logger;
    private readonly ISignEffectiveBalanceResolver _signEffectiveBalanceResolver;

    public ComputingCore(IRewardCycleResolver rewardCycleResolver, IBalanceSnapshotResolver balanceSnapshotResolver,
        IBalanceChangeResolver balanceChangeResolver, IEffectiveBalanceResolver effectiveBalanceResolver,
        ISignEffectiveBalanceResolver signEffectiveBalanceResolver,
        ILogger<ComputingCore> logger)
    {
        _signEffectiveBalanceResolver = signEffectiveBalanceResolver;
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
            await _rewardCycleResolver.CheckRewardCycle(rewardCycle);
        }
    }

    private async Task CalculateOneCycleEffectiveBalance(RewardCycle rewardCycle)
    {
        _logger.LogInformation(
            $"Start calculating effective balances for cycle - Era: {rewardCycle.EndEraIndex} StartBlock: {rewardCycle.StartBlock}.");

        try
        {
            await _effectiveBalanceResolver.RemoveEffectiveBalanceInBlocksRange(rewardCycle.StartBlock,
                rewardCycle.EndBlock);

            await _balanceSnapshotResolver.CalculateBalanceSnapshotInACycle(rewardCycle);

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
            {
                await _effectiveBalanceResolver.SaveEffectiveBalances(effectiveBalancesList);
                await _signEffectiveBalanceResolver.Resolve(rewardCycle, effectiveBalancesList);
                await _signEffectiveBalanceResolver.SignData();
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