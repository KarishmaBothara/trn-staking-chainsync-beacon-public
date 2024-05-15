using System.Numerics;
using MatrixEngine.Core.Exceptions;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.DTOs;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.Resolvers;

public interface IBalanceSnapshotResolver
{
    Task CalculateBalanceSnapshotInACycle(RewardCycle cycle);

    /// <summary>
    /// This is only for the first cycle, calculate the genesis validator balance as balance snapshot
    /// </summary>
    /// <param name="cycle"></param>
    Task CalculateGenesisBalanceSnapshot(RewardCycle cycle);

    Task CalculateBalanceSnapshot(RewardCycle cycle);
}

public class BalanceSnapshotResolver : IBalanceSnapshotResolver
{
    private readonly IRewardCycleService _rewardCycleService;
    private readonly IBalanceSnapshotService _balanceSnapshotService;
    private readonly IGenesisValidatorService _genesisValidatorService;
    private readonly IBalanceChangeService _balanceChangeService;
    private readonly ILogger<BalanceSnapshotResolver> _logger;

    public BalanceSnapshotResolver(IRewardCycleService rewardCycleService,
        IBalanceSnapshotService balanceSnapshotService, IGenesisValidatorService genesisValidatorService,
        IBalanceChangeService balanceChangeService, ILogger<BalanceSnapshotResolver> logger)
    {
        _logger = logger;
        _balanceChangeService = balanceChangeService;
        _genesisValidatorService = genesisValidatorService;
        _balanceSnapshotService = balanceSnapshotService;
        _rewardCycleService = rewardCycleService;
    }

    /// <summary>
    /// It is called before the effective balance calculation, if the effective balances do not exist in the database.
    /// TODO: Recursively build balance snapshot for previous cycles if the previous cycle does not have balance snapshot
    /// </summary>
    /// <param name="cycle"></param>
    public async Task CalculateBalanceSnapshotInACycle(RewardCycle cycle)
    {
        _logger.LogInformation($"Starting calculate balance snapshot for cycle {cycle.StartBlock}");

        //check if this cycle has balance base, which means last cycle's balance snapshot has been built
        var hasBaseBalance = await _balanceSnapshotService.HasCycleHaveBaseBalance(cycle.StartBlock);

        if (hasBaseBalance)
        {
            //no need to build the balance snapshot for previous
            _logger.LogInformation($"cycle {cycle.StartBlock} has base balance snapshot already. Skip.");
            return;
        }

        var isFirstCycle = await _rewardCycleService.IsRewardCycleTheFirstCycle(cycle.StartBlock);
        if (isFirstCycle)
        {
            _logger.LogInformation($"Cycle {cycle.StartBlock} is the first cycle. Calculate genesis balance snapshot.");
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
            _logger.LogInformation($"Cycle {cycle.StartBlock} is not the first cycle. Calculate balance snapshot.");
            await CalculateBalanceSnapshot(cycle);
        }
    }

    /// <summary>
    /// This is only for the first cycle, calculate the genesis validator balance as balance snapshot
    /// </summary>
    /// <param name="cycle"></param>
    public async Task CalculateGenesisBalanceSnapshot(RewardCycle cycle)
    {
        _logger.LogInformation($"Starting calculate genesis balance snapshot for cycle {cycle.StartBlock}");
        // fetch genesis validator balance
        var genesisValidators = await _genesisValidatorService.GetGenesisValidators();
        // create base balance by each genesis validator, stash to account, lockedBalance to balance, block use the cycle.startBlock - 1
        _logger.LogInformation($"Loaded {genesisValidators.Count} genesis validators.");
        if (genesisValidators.Count == 0)
        {
            _logger.LogWarning("No genesis validators found.");
            return;
        }

        var balanceSnapshots = genesisValidators.Select(validator => new BalanceSnapshotModel
        {
            Account = validator.Stash,
            Balance = validator.LockedBalance,
            EndBlock = cycle.StartBlock - 1
        }).ToList();

        _logger.LogInformation($"Upserting {balanceSnapshots.Count} genesis balance snapshots.");
        await _balanceSnapshotService.UpsertBalanceSnapshots(balanceSnapshots);
    }


    public async Task CalculateBalanceSnapshot(RewardCycle cycle)
    {
        _logger.LogInformation($"Starting calculate balance snapshot for cycle {cycle.StartBlock}");
        // use cycle.Start - 1  =  previous cycle's endBlock
        var previousEndBlock = cycle.StartBlock - 1;
        _logger.LogInformation($"Get Previous Cycle by End Block: {previousEndBlock}");
        var previousCycle = await _rewardCycleService.GetRewardCycleByEndBlock(previousEndBlock);
        var hasBaseBalance = await _balanceSnapshotService.HasCycleHaveBaseBalance(previousCycle.StartBlock);
        _logger.LogInformation(
            $"Previous Cycle (start block:{previousCycle.StartBlock}) has base balance: {hasBaseBalance}");

        if (hasBaseBalance)
        {
            // get previous cycle balance snapshot as base balance
            var previousCycleBalanceSnapshot =
                await _balanceSnapshotService.GetBalanceSnapshotByCycleEndBlock(previousCycle.StartBlock - 1);

            _logger.LogInformation($"Loaded Previous Cycle Base balance ${previousCycleBalanceSnapshot.Count} records");
            // fetch every users' last balance change (largest block number balance change)
            var lastBalanceChanges =
                await _balanceChangeService.GetUsersLastBalanceChanges(previousCycle.StartBlock,
                    previousCycle.EndBlock);

            _logger.LogInformation($"Loaded Last Balance Changes {lastBalanceChanges.Count} records");

            //get all accounts from previousCycleBalanceSnapshot and lastBalanceChanges, then remove duplications
            var accounts = previousCycleBalanceSnapshot.Select(x => x.Account)
                .Union(lastBalanceChanges.Select(x => x.Account)).Distinct().ToList();

            _logger.LogInformation($"Distinct Accounts {accounts.Count} records");
            //loop accounts to build balance snapshot
            var balanceSnapshots = accounts.Select(account =>
                {
                    var baseBalanceModel = previousCycleBalanceSnapshot.FirstOrDefault(f => f.Account == account);
                    var latestBalanceChangeModel = lastBalanceChanges.FirstOrDefault(f => f.Account == account);

                    var baseBalance = BigInteger.Parse(baseBalanceModel?.Balance ?? "0");

                    var latestBalanceChange = BigInteger.Parse(latestBalanceChangeModel?.BalanceChange ?? "0");
                    return new BalanceSnapshotModel
                    {
                        Account = account,
                        Balance = (baseBalance + latestBalanceChange).ToString(),
                        EndBlock = cycle.StartBlock - 1
                    };
                }
            ).ToList();

            _logger.LogInformation(
                $"Upserting {balanceSnapshots.Count} balance snapshots for cycle {cycle.StartEraIndex} - {cycle.EndEraIndex}.");
            await _balanceSnapshotService.UpsertBalanceSnapshots(balanceSnapshots);
        }
        else
        {
            //We don't recursively calculate the snapshot if previous cycle has no balance snapshot
            _logger.LogInformation(
                $"The previous cycle blocks({cycle.StartBlock}-{cycle.EndBlock}) does not have base balance snapshot.");
            throw new BalanceSnapshotException(
                $"The previous cycle blocks({cycle.StartBlock}-{cycle.EndBlock}) does not have base balance snapshot.");
        }
    }
}