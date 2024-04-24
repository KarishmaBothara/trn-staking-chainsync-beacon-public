using System.Numerics;
using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.Events;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.Resolvers;

public interface IBalanceChangeResolver
{
    Task<Dictionary<string, List<BalanceChangeModel>>> ResolveBalanceChange(
        int startBlock,
        int endBlock);

    Task<Dictionary<string, List<BalanceChangeModel>>> ResolveBalanceChangeWithTransactions(
        List<TransactionModel> transactions,
        int startBlock,
        int endBlock);

    Task<Dictionary<string, List<BalanceChangeModel>>> ResolveBalanceChangesWithBalanceSnapshots(
        List<BalanceSnapshotModel> balanceSnapshots,
        List<TransactionModel> transactions,
        int startBlock,
        int endBlock,
        List<EraModel> erasInCycle);

    Task<Dictionary<string, List<BalanceChangeModel>>> CalculateBalanceChanges(
        List<BalanceSnapshotModel> balanceSnapshots,
        IEnumerable<TransactionModel> transactions,
        int startBlock,
        int endBlock);

    Task<Dictionary<string, List<BalanceChangeModel>>> SplitBalanceChangesAcrossEras(
        Dictionary<string, List<BalanceChangeModel>> accountBalanceChanges,
        List<EraModel> erasInCycle);

    Dictionary<string, List<BalanceChangeModel>> ApplyPunishmentForBalanceChanges(
        Dictionary<string, List<BalanceChangeModel>> adjustedBalanceChanges);
}

public class BalanceChangeResolver : IBalanceChangeResolver
{
    private readonly IBalanceSnapshotService _balanceSnapshotService;
    private readonly IEraService _eraService;
    private readonly ITransactionEventService _transactionEventService;
    private readonly IStakerService _stakerService;
    private readonly ILogger<BalanceChangeResolver> _logger;
    private IBalanceChangeService _balanceChangeService;

    public BalanceChangeResolver(IBalanceSnapshotService balanceSnapshotService,
        IEraService eraService, ITransactionEventService transactionEventService,
        IStakerService stakerService, IBalanceChangeService balanceChangeService, ILogger<BalanceChangeResolver> logger)
    {
        _logger = logger;
        _stakerService = stakerService;
        _transactionEventService = transactionEventService;
        _eraService = eraService;
        _balanceSnapshotService = balanceSnapshotService;
        _balanceChangeService = balanceChangeService;
    }

    public async Task<Dictionary<string, List<BalanceChangeModel>>> ResolveBalanceChange(int startBlock, int endBlock)
    {
        _logger.LogInformation($"Start resolving balance changes from block {startBlock} to {endBlock}");
        var transactions = await _transactionEventService.GetTransactionEventsByBlockRange(startBlock, endBlock);
        return await ResolveBalanceChangeWithTransactions(transactions, startBlock, endBlock);
    }

    public async Task<Dictionary<string, List<BalanceChangeModel>>> ResolveBalanceChangeWithTransactions(
        List<TransactionModel> transactions,
        int startBlock,
        int endBlock)
    {
        var previousBlock = startBlock - 1;
        var balanceSnapshots = await _balanceSnapshotService.GetBalanceSnapshotByEndBlock(previousBlock);
        _logger.LogInformation($"Loaded {balanceSnapshots?.Count} balance snapshots from block {previousBlock}.");
        var eras = await _eraService.GetEraListByBlockRange(startBlock, endBlock);
        _logger.LogInformation($"Loaded {eras?.Count} eras from block {startBlock} to {endBlock}.");

        return await ResolveBalanceChangesWithBalanceSnapshots(balanceSnapshots, transactions, startBlock, endBlock,
            eras);
    }

    // Resolve balance changes from balance snapshot and transaction events
    // all balance snapshots can be deemed as the beginning deposit of the account
    // if user does not have a balance snapshot, the beginning balance is 0
    // then use the below CalculateBalanceChanges method to calculate the balance changes
    // then use the below SplitBalanceChangesAcrossEras method to split the balance changes across eras
    // then use the below ApplyPunishmentForBalanceChanges method to apply punishment for balance changes
    public async Task<Dictionary<string, List<BalanceChangeModel>>> ResolveBalanceChangesWithBalanceSnapshots(
        List<BalanceSnapshotModel> balanceSnapshots,
        List<TransactionModel> transactions,
        int startBlock,
        int endBlock,
        List<EraModel> erasInCycle)
    {
        // TODO: Refactor this part to check if values are nulls.
        // Calculate the balance changes from the transactions
        var balanceChanges = await CalculateBalanceChanges(balanceSnapshots, transactions, startBlock, endBlock);
        _logger.LogInformation($"Loaded {balanceChanges?.Keys.Count ?? 0} users' balance changes.");

        // Split the balance changes across eras
        var splitBalanceChanges = await SplitBalanceChangesAcrossEras(balanceChanges, erasInCycle);
        _logger.LogInformation($"Loaded {splitBalanceChanges?.Keys.Count ?? 0} users' split balance changes.");

        // Apply punishment for the balance changes
        var applyPunishmentForBalanceChanges = ApplyPunishmentForBalanceChanges(splitBalanceChanges);
        _logger.LogInformation(
            $"Loaded {applyPunishmentForBalanceChanges?.Keys.Count ?? 0} users' effective balance changes.");

        return applyPunishmentForBalanceChanges;
    }

    public async Task<Dictionary<string, List<BalanceChangeModel>>> CalculateBalanceChanges(
        List<BalanceSnapshotModel> balanceSnapshots,
        IEnumerable<TransactionModel> transactions,
        int startBlock,
        int endBlock)
    {
        _logger.LogInformation($"Calculating balance changes from block {startBlock} to {endBlock}.");
        var accountBalances = new Dictionary<string, BigInteger>();
        var accountBalanceChanges = new Dictionary<string, List<BalanceChangeModel>>();

        // Filter transactions by the specified block range before sorting
        var filteredSortedTransactions = transactions
            .Where(t => t.BlockNumber >= startBlock && t.BlockNumber <= endBlock)
            .OrderBy(t => t.BlockNumber)
            .ToList();

        foreach (var transaction in filteredSortedTransactions)
        {
            if (!accountBalances.ContainsKey(transaction.Account))
            {
                var snapshot = balanceSnapshots.FirstOrDefault(s => s.Account == transaction.Account);
                accountBalances[transaction.Account] = snapshot != null
                    ? BigInteger.Parse(snapshot.Balance ?? BigInteger.Zero.ToString())
                    : BigInteger.Zero;
                accountBalanceChanges[transaction.Account] = new List<BalanceChangeModel>();
            }

            var balanceChange = string.Equals(TransactionType.Withdrawn, transaction.Type,
                StringComparison.CurrentCultureIgnoreCase)
                ? -BigInteger.Parse(transaction.Amount)
                : BigInteger.Parse(transaction.Amount);
            // Calculate the new total balance after this transaction
            var previousBalance = accountBalances[transaction.Account];
            var newTotalBalance = accountBalances[transaction.Account] + balanceChange;

            // Find the next transaction for this account within the block range to determine the end block
            var nextTransactionIndex = filteredSortedTransactions.FindIndex(t =>
                t.Account == transaction.Account && t.BlockNumber > transaction.BlockNumber);
            var endBlockForChange = (nextTransactionIndex != -1)
                ? filteredSortedTransactions[nextTransactionIndex].BlockNumber - 1
                : endBlock;

            accountBalanceChanges[transaction.Account].Add(new BalanceChangeModel
            {
                Account = transaction.Account,
                PreviousBalance = previousBalance,
                BalanceChange = balanceChange,
                BalanceInBlockRange = newTotalBalance,
                StartBlock = transaction.BlockNumber,
                EndBlock = endBlockForChange
            });

            // Update the account balance
            accountBalances[transaction.Account] = newTotalBalance;
        }
        
        // save the balance changes to the database
        var allBalanceChanges = accountBalanceChanges?.Values.SelectMany(x => x).ToList();
        if (allBalanceChanges != null) await _balanceChangeService.UpsertUserBalanceChanges(allBalanceChanges);

        return accountBalanceChanges;
    }

    public async Task<Dictionary<string, List<BalanceChangeModel>>> SplitBalanceChangesAcrossEras(
        Dictionary<string, List<BalanceChangeModel>> accountBalanceChanges,
        List<EraModel> erasInCycle)
    {
        _logger.LogInformation($"Splitting balance changes across eras.");
        var adjustedBalanceChanges = new Dictionary<string, List<BalanceChangeModel>>();

        var accounts = accountBalanceChanges.Keys.ToList();
        var stakerTypes = await _stakerService.GetAccountsStakerTypesByEraIndexes(accounts,
            erasInCycle.Select(c => c.EraIndex).ToList());

        // TODO: Refactor this part to improve readability
        var tasks = new List<Task<Tuple<string, List<BalanceChangeModel>>>>();
        foreach (var accountEntry in accountBalanceChanges)
        {
            var account = accountEntry.Key;

            var task = Task.Run(() =>
                LoopUserAdjustedBalanceChanges(erasInCycle, accountEntry, account, stakerTypes));

            tasks.Add(task);
        }

        // run in parallel to improve performance
        // notice: if too many tasks are running in parallel, it may cause performance/memory issue
        _logger.LogInformation($"Running {tasks.Count} tasks in parallel to split balances if cross eras.");
        await Task.WhenAll(tasks);

        foreach (var task in tasks)
        {
            adjustedBalanceChanges[task.Result.Item1] = task.Result.Item2;
        }

        return adjustedBalanceChanges;
    }

    private Tuple<string, List<BalanceChangeModel>> LoopUserAdjustedBalanceChanges(
        List<EraModel> erasInCycle, KeyValuePair<string, List<BalanceChangeModel>> accountEntry, string account,
        List<StakerModel> stakerTypes)
    {
        var userAdjustedBalanceChanges = UserAdjustedBalanceChanges(erasInCycle, accountEntry, account, stakerTypes);
        return new Tuple<string, List<BalanceChangeModel>>(account, userAdjustedBalanceChanges);
    }

    private List<BalanceChangeModel> UserAdjustedBalanceChanges(List<EraModel> erasInCycle,
        KeyValuePair<string, List<BalanceChangeModel>> accountEntry, string account,
        List<StakerModel> stakerTypes)
    {
        _logger.LogInformation($"Splitting balance changes for account {account}.");
        var userAdjustedBalanceChanges = new List<BalanceChangeModel>();
        foreach (var change in accountEntry.Value)
        {
            var erasSpanned = erasInCycle
                .Where(e => e.StartBlock <= change.EndBlock && e.EndBlock >= change.StartBlock).ToList();
            foreach (var era in erasSpanned)
            {
                var startBlock = Math.Max(change.StartBlock, era.StartBlock);
                var endBlock = Math.Min(change.EndBlock, era.EndBlock);

                var effectiveBlocks = endBlock - startBlock + 1;
                var totalBlocksInEra = era.EndBlock - era.StartBlock + 1;
                var effectiveEras = new decimal(effectiveBlocks) / new decimal(totalBlocksInEra);

                var stakerType = GetStakerType(account, stakerTypes, era.EraIndex);

                userAdjustedBalanceChanges.Add(new BalanceChangeModel
                    {
                        Account = account,
                        BalanceChange = change.BalanceChange,
                        BalanceInBlockRange = change.BalanceInBlockRange,
                        StartBlock = startBlock,
                        EndBlock = endBlock,
                        EraIndex = era.EraIndex,
                        EffectiveBlocks = effectiveBlocks,
                        EffectiveEras = effectiveEras,
                        StakerType = stakerType
                    }
                );
            }
        }

        return userAdjustedBalanceChanges;
    }

    private static string? GetStakerType(string account, List<StakerModel> stakerTypes, int eraIndex)
    {
        var stakerModel = stakerTypes.Find(x =>
            x.Account == account && x.EraIndex == eraIndex &&
            (x.Type == StakerType.Validator || x.Type == StakerType.Nominator));
        var stakerType = stakerModel != null ? stakerModel.Type : StakerType.Staker;
        return stakerType;
    }

    public Dictionary<string, List<BalanceChangeModel>> ApplyPunishmentForBalanceChanges(
        Dictionary<string, List<BalanceChangeModel>> adjustedBalanceChanges)
    {
        _logger.LogInformation($"Apply punishment for {adjustedBalanceChanges?.Keys?.Count ?? 0} balance changes.");
        var effectiveBalanceChanges = new Dictionary<string, List<BalanceChangeModel>>();

        foreach (var accountEntry in adjustedBalanceChanges)
        {
            string accountId = accountEntry.Key;
            var balanceChanges = accountEntry.Value;

            // Order the balance changes by end block number in descending order
            var orderedBalanceChanges = balanceChanges.OrderByDescending(c => c.EndBlock).ToList();

            BigInteger lowestBalance = orderedBalanceChanges.First().BalanceInBlockRange;
            List<BalanceChangeModel> modifiedBalanceChanges = new List<BalanceChangeModel>();

            // Initially set lowestBalance to the first change's total balance
            foreach (var change in orderedBalanceChanges)
            {
                var currentBalance = change.BalanceInBlockRange;

                // If the current balance is lower, update lowestBalance
                if (currentBalance < lowestBalance)
                {
                    lowestBalance = currentBalance;
                }

                // Modify the change's totalBalance to reflect the lowest balance if needed
                modifiedBalanceChanges.Add(new BalanceChangeModel
                {
                    Account = change.Account,
                    EraIndex = change.EraIndex,
                    EffectiveBlocks = change.EffectiveBlocks,
                    EffectiveEras = change.EffectiveEras,
                    BalanceChange = change.BalanceChange,
                    BalanceInBlockRange = lowestBalance,
                    StartBlock = change.StartBlock,
                    EndBlock = change.EndBlock,
                    StakerType = change.StakerType
                });
            }

            // Ensure all changes are included but with adjusted TotalBalance where needed
            effectiveBalanceChanges[accountId] = modifiedBalanceChanges;
        }

        return effectiveBalanceChanges;
    }

}