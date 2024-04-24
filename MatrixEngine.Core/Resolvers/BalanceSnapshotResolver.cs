using MatrixEngine.Core.Models;
using MatrixEngine.Core.Services;

namespace MatrixEngine.Core.Resolvers;

public interface IBalanceSnapshotResolver
{
    Task CalculateBalanceSnapshotInACycle(RewardCycle cycle);
}

public class BalanceSnapshotResolver : IBalanceSnapshotResolver
{
    private readonly IRewardCycleService _rewardCycleService;
    private readonly IBalanceSnapshotService _balanceSnapshotService;
    private IGenesisValidatorService _genesisValidatorService;

    public BalanceSnapshotResolver(IRewardCycleService rewardCycleService,
        IBalanceSnapshotService balanceSnapshotService, IGenesisValidatorService genesisValidatorService)
    {
        _genesisValidatorService = genesisValidatorService;
        _balanceSnapshotService = balanceSnapshotService;
        _rewardCycleService = rewardCycleService;
    }

    /// <summary>
    /// It is called before the effective balance calculation, if the effective balances do not exist in the database.
    /// </summary>
    /// <param name="cycle"></param>
    public async Task CalculateBalanceSnapshotInACycle(RewardCycle cycle)
    {
        var isFirstCycle = await _rewardCycleService.IsRewardCycleTheFirstCycle(cycle.StartBlock);
        if (isFirstCycle)
        {
            await CalculateGenesisBalanceSnapshot(cycle);
        }
        else
        {
            // e.g. We have A, B, C three cycles and C is the current cycle
            // to calculate C's effective balance we need Cycle B's balance snapshot as base balance
            // but if Cycle B's balance snapshot does not exist, we need to calculate it first
            // which means it needs to fetch A's balance snapshot as base balance to calculate B's balance snapshot
            // hence, it will recursively calculate the balance snapshot 
            // However, theoretically, as we check and calculate the snapshot every cycles.
            // It won't happen that the previous balance snapshot does not exist in the database.
            // We may not need to calculate the balance snapshot recursively.
            await CalculateBalanceSnapshot(cycle);
        }
    }

    /// <summary>
    /// This is only for the first cycle, calculate the genesis validator balance as balance snapshot
    /// </summary>
    /// <param name="cycle"></param>
    private async Task CalculateGenesisBalanceSnapshot(RewardCycle cycle)
    {
        // fetch genesis validator balance
        var genesisValidators = await _genesisValidatorService.GetGenesisValidators();
        // create base balance by each genesis validator, stash to account, lockedBalance to balance, block use the cycle.startBlock - 1
        var balanceSnapshots = genesisValidators.Select(validator => new BalanceSnapshotModel
        {
            Account = validator.Stash,
            Balance = validator.LockedBalance,
            EndBlock = cycle.StartBlock - 1
        }).ToList();

        await _balanceSnapshotService.UpsertBalanceSnapshots(balanceSnapshots);
    }


    private async Task CalculateBalanceSnapshot(RewardCycle cycle)
    {
        // use cycle.Start - 1  =  previous cycle's endBlock
        var previousEndBlock = cycle.StartBlock - 1;
        var previousCycle = _rewardCycleService.GetRewardCycleByEndBlock(previousEndBlock);
        var hasBaseBalance = await _balanceSnapshotService.HasCycleHaveBaseBalance(previousCycle);
        if (hasBaseBalance)
        {
            //start build balance snapshot base one endblock == previousCycle
        }
        // to query cycle
        // get the previous cycle start block and end block
        // get the balance base of the previous cycle by query endBlock = previousCycle.startBlock - 1

        // fetch balance changes in the cycle
        // calculate new balance snapshot
        // save to db
        throw new NotImplementedException();
    }
}