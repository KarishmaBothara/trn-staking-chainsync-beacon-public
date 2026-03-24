using System.Numerics;
using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.DTOs;
using MatrixEngine.Core.Models.Events;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.Resolvers;

public interface IBalanceChangeResolver
{
    Task ResolveBalanceChange(int startBlock, int endBlock);
    
    // Gets stored balance changes from the DB and formats by account
    Task<Dictionary<string, List<BalanceChangeModel>>> GetBalanceChangesInRange(int startBlock, int endBlock);
}

// The BalanceChangeResolver is responsible for calculating the balance changes and StakerType of an account within a reward cycle
// This is achieved by calculating the balance changes from the Withdraw and Bonded transaction events for each account
// within the reward cycle. The staker type of the account is determined by the era index of the end block of the balance change
// The balance changes are then saved to the database
public class BalanceChangeResolver : IBalanceChangeResolver
{
    private readonly IEraService _eraService;
    private readonly ITransactionEventService _transactionEventService;
    private readonly IChilledService _chilledService;
    private readonly IStakerService _stakerService;
    private readonly ILogger<BalanceChangeResolver> _logger;
    private readonly IBalanceChangeService _balanceChangeService;
    private readonly IEffectiveBalanceResolver _effectiveBalanceResolver;

    public BalanceChangeResolver(
        IEraService eraService, 
        ITransactionEventService transactionEventService,
        IChilledService chilledService,
        IStakerService stakerService, 
        IBalanceChangeService balanceChangeService, 
        IEffectiveBalanceResolver effectiveBalanceResolver,
        ILogger<BalanceChangeResolver> logger
    )
    {
        _logger = logger;
        _stakerService = stakerService;
        _transactionEventService = transactionEventService;
        _chilledService = chilledService;
        _eraService = eraService;
        _balanceChangeService = balanceChangeService;
        _effectiveBalanceResolver = effectiveBalanceResolver;
    }
    
    // Get the balance changes from the database and format them by account
    // Sorts the balance changes by start block
    public async Task<Dictionary<string, List<BalanceChangeModel>>> GetBalanceChangesInRange(
        int startBlock, 
        int endBlock
    )
    {
        _logger.LogInformation($"Getting balance changes from block {startBlock} to {endBlock}");
        // Get all balance changes within the block range
        var balanceChanges = await _balanceChangeService.GetBalanceChangesInRange(startBlock, endBlock);
        // Group by account and map BalanceModel to BalanceChangeModel
        var balanceChangesByAccount = balanceChanges
            .GroupBy(balanceModel => balanceModel.Account)
            .ToDictionary(
                group => group.Key,
                group => group.Select(balanceModel => new BalanceChangeModel
                {
                    Account = balanceModel.Account,
                    StartBlock = balanceModel.StartBlock,
                    EndBlock = balanceModel.EndBlock,
                    Bonded = new BalanceChangeDetail(
                        BigInteger.Parse(balanceModel.Bonded.PreviousBalance),
                        BigInteger.Parse(balanceModel.Bonded.BalanceChange), 
                        BigInteger.Parse(balanceModel.Bonded.BalanceInBlockRange),
                        balanceModel.Bonded.StakerType?? StakerType.Staker
                    ),
                    Unlocking = new BalanceChangeDetail(
                        BigInteger.Parse(balanceModel.Unlocking.PreviousBalance), 
                        BigInteger.Parse(balanceModel.Unlocking.BalanceChange),
                        BigInteger.Parse(balanceModel.Unlocking.BalanceInBlockRange),
                        StakerType.Staker
                    )
                }).OrderBy(balanceModel => balanceModel.StartBlock).ToList()
            );
        
        return balanceChangesByAccount;
    }
    
    public async Task ResolveBalanceChange(
        int startBlock, 
        int endBlock
    )
    {
        _logger.LogInformation($"Start resolving balance changes from block {startBlock} to {endBlock}");
        // Get end balances of previous cycle
        List<EffectiveBalanceModel> previousCycleEffectiveBalances = await _effectiveBalanceResolver.GetPreviousCycleEffectiveBalances(startBlock);
        // Get all transactions within this cycle
        var transactions = await _transactionEventService.GetTransactionEventsByBlockRange(startBlock, endBlock);
        // Get all eras that fall within this cycle
        var eras = await _eraService.GetEraListByBlockRange(startBlock, endBlock);
        // Get all chilled events that fall within this cycle
        var chilledEvents = await _chilledService.GetChilledEventsByBlockRange(startBlock, endBlock);
        _logger.LogInformation($"Loaded {eras?.Count} eras, {chilledEvents?.Count ?? 0} chilled events and {transactions?.Count ?? 0} transactions from block {startBlock} to {endBlock}.");
        
        // Calculate balance changes
        var balanceChanges = CalculateBalanceChanges(previousCycleEffectiveBalances, transactions, startBlock, endBlock);
        // Get staker type for each account at the index
        var balanceChangesWithStakerType = await GetStakerTypeForBalanceChanges(balanceChanges, eras, chilledEvents);
        
        // save the balance changes to the database
        var allBalanceChanges = balanceChangesWithStakerType?.Values.SelectMany(x => x).ToList();
        if (allBalanceChanges != null) await _balanceChangeService.UpsertUserBalanceChanges(allBalanceChanges);
    }

    // Calculate balance changes from an accounts Withdraw and Bonded transaction events within a reward cycle
    public Dictionary<string, List<BalanceChangeModel>> CalculateBalanceChanges(
        List<EffectiveBalanceModel> previousRewardCycleBalances,
        List<TransactionModel> transactions,
        int startBlock,
        int endBlock
    )
    {
        _logger.LogInformation($"Calculating balance changes from block {startBlock} to {endBlock}.");
        var accountBondedBalances = new Dictionary<string, BigInteger>();
        var accountUnlockingBalances = new Dictionary<string, BigInteger>();
        var accountBalanceChanges = new Dictionary<string, List<BalanceChangeModel>>();

        // go through all previous reward cycle balances and add existing bonded and unlocking balances
        // to the accountBondedBalances and accountUnlockingBalances dictionaries
        foreach (var prevBalance in previousRewardCycleBalances)
        {
            var account = prevBalance.Account;
            BigInteger bondedBalance = BigInteger.Parse(prevBalance.Bonded.Balance);
            BigInteger unlockingBalance = BigInteger.Parse(prevBalance.Unlocking.Balance);
            accountBondedBalances[account] = bondedBalance;
            accountUnlockingBalances[account] = unlockingBalance;
            if (bondedBalance == 0 && unlockingBalance == 0)
            {
                // No need to create a balance change model if there is no balance
                continue;
            }
            var transactionModel = new TransactionModel
            {
                Account = account,
                BlockNumber = startBlock,
                Amount = bondedBalance.ToString(),
                Type = TransactionType.Carryover
            };
            transactions.Add(transactionModel);
            accountBalanceChanges[account] = new List<BalanceChangeModel>();
        }
        
        // Filter transactions by the specified block range
        // Group by Account and BlockNumber to account for the edge case where there are more than one transaction in a single block
        var filteredTransactions = transactions
            .Where(t => t.BlockNumber >= startBlock && t.BlockNumber <= endBlock)
            .GroupBy(t => new { t.Account, t.BlockNumber })
            .Select(g => g.ToList())
            .OrderBy(g => g.First().BlockNumber)
            .ToList();
        
        foreach (var transaction in filteredTransactions)
        {
            // All accounts in a group should be the same
            var account = transaction.First().Account;
            
            if (!accountBalanceChanges.ContainsKey(account))
            {
                accountBondedBalances[account] = BigInteger.Zero;
                accountUnlockingBalances[account] = BigInteger.Zero;
                accountBalanceChanges[account] = new List<BalanceChangeModel>();
            }

            var balanceChangeModel = BuildNewBalanceChangeFromTransactionChanges(
                account,
                endBlock, 
                transaction, 
                accountBondedBalances,
                accountUnlockingBalances,
                filteredTransactions
            );
            accountBalanceChanges[account].Add(balanceChangeModel);

            // Update the account balance for bonded and unlocking
            accountBondedBalances[account] = balanceChangeModel.Bonded.BalanceInBlockRange;
            accountUnlockingBalances[account] = balanceChangeModel.Unlocking.BalanceInBlockRange;
        }
        
        return accountBalanceChanges;
    }

    private BalanceChangeModel BuildNewBalanceChangeFromTransactionChanges(
        string account,
        int endBlock,
        List<TransactionModel> transactions, 
        Dictionary<string, BigInteger> accountBondedBalances,
        Dictionary<string, BigInteger> accountUnlockingBalances,
        List<List<TransactionModel>> allTransactions
    )
    {
        // Should not happen
        if (transactions.Count == 0)
        {
            throw new Exception("No transactions found for account: " + account);
        }
        
        
        // Calculate the new total balance after this transaction
        var previousBondedBalance = accountBondedBalances[account];
        var previousUnlockingBalance = accountUnlockingBalances[account];
        var bondedChange = BigInteger.Zero;
        var unlockingChange = BigInteger.Zero;
        var blockNumber = transactions.First().BlockNumber;
        // Go through each transaction if there are multiple
        foreach (var transaction in transactions)
        {
            BigInteger amount = BigInteger.Parse(transaction.Amount);
            // Determine change in bonded and unlocking values
            switch (transaction.Type)
            {
                case TransactionType.Bonded:
                    // Amount was bonded externally
                    bondedChange += amount;
                    break;
                case TransactionType.ReBonded:
                    // Amount was rebonded, take out of unlocking
                    bondedChange += amount;
                    unlockingChange -= amount;
                    break;
                case TransactionType.Unbonded:
                    // Amount was unbonded, take out of bonded and add to unlocking
                    bondedChange -= amount;
                    unlockingChange += amount;
                    break;
                case TransactionType.Withdrawn:
                    // Amount was withdrawn, take out of unlocking
                    unlockingChange -= amount;
                    break;
                case TransactionType.Slashed:
                    // Amount was slashed, attempt to reduce out of bonded first
                    // If bonded is not enough, reduce out of unlocking
                    // Saturate to prevent negatives
                    var bondedDiff = previousBondedBalance - amount;
                    if (bondedDiff < 0)
                    {
                        bondedChange -= previousBondedBalance;
                        unlockingChange -= BigInteger.Min(amount - previousBondedBalance, previousUnlockingBalance);
                    } else {
                        bondedChange -= amount;
                    }
                    break;
                case TransactionType.Carryover:
                    // Amount was carried over from the previous cycle
                    // change is zero as it is already added to the accounts total
                    // in accountBondedBalances and accountUnlockingBalances
                    break;
            }
        }

        var endBlockForChange = GetEndBlockForTransaction(
            blockNumber, 
            account,
            allTransactions
        )?? endBlock;

        // Should not happen in real world scenario
        if (previousBondedBalance + bondedChange < 0)
        {
            _logger.LogError($"Bonded balance cannot be negative for {account}, prevBalance: {previousBondedBalance}, change: {bondedChange}");
        }
        if (previousUnlockingBalance + unlockingChange < 0)
        {
            _logger.LogError("Unlocking balance cannot be negative for account: " + account);
        }       
        
        // Construct balance details for bonded and unlocking balances
        var bondedBalanceDetail = new BalanceChangeDetail(
            previousBondedBalance,
            bondedChange,
            previousBondedBalance + bondedChange
        );
        var unlockingBalanceDetail = new BalanceChangeDetail(
            previousUnlockingBalance,
            unlockingChange,
            previousUnlockingBalance + unlockingChange
        );
        var balanceChangeModel = new BalanceChangeModel
        {
            Account = account,
            StartBlock = blockNumber,
            EndBlock = endBlockForChange,
            Bonded = bondedBalanceDetail,
            Unlocking = unlockingBalanceDetail,
        };
        return balanceChangeModel;
    }

    // // Find the next transaction for this account within the block range to determine the end block
    // private static int? GetEndBlockForTransaction(
    //     int blockNumber, 
    //     string account,
    //     List<TransactionModel> filteredSortedTransactions
    // )
    // {
    //     var nextTransactionIndex = filteredSortedTransactions.FindIndex(t =>
    //         t.Account == account && t.BlockNumber > blockNumber);
    //     if (nextTransactionIndex == -1) return null;
    //     return filteredSortedTransactions[nextTransactionIndex].BlockNumber - 1;
    // }
    
    // Find the next transaction for this account within the block range to determine the end block
    private static int? GetEndBlockForTransaction(
        int blockNumber, 
        string account,
        List<List<TransactionModel>> filteredSortedTransactions
    )
    {
        foreach (var group in filteredSortedTransactions)
        {
            // All transactions in a group share the same account and block number
            var firstTransaction = group.First();
            if (firstTransaction.Account == account && firstTransaction.BlockNumber > blockNumber)
            {
                return firstTransaction.BlockNumber - 1;
            }
        }

        return null;
    }
    
    // Gets Staker type for each account based on the era index of the end block of the balance change
    private async Task<Dictionary<string, List<BalanceChangeModel>>> GetStakerTypeForBalanceChanges(
        Dictionary<string, List<BalanceChangeModel>> balanceChanges,
        List<EraModel> erasInCycle,
        List<ChilledModel> chilledEvents
    )
    {
        var accounts = balanceChanges.Keys.ToList();
        var erasInCycleIndexes = erasInCycle.Select(c => c.EraIndex).ToList();
        // Get all staker types within this cycle
        var stakerTypes = await _stakerService.GetAccountsStakerTypesByEraIndexes(accounts, erasInCycleIndexes);
        Dictionary<string, List<BalanceChangeModel>> finalBalanceChanges = new Dictionary<string, List<BalanceChangeModel>>();

        foreach (var accountEntry in balanceChanges)
        {
            List<BalanceChangeModel> splitBalanceChanges = new List<BalanceChangeModel>();
            var account = accountEntry.Key;
            var accountBalanceChanges = accountEntry.Value;
            // Cache the staker types per account to prevent multiple lookups
            // The Staker Types can be very large, so simplifying to only repeatedly look through for entries of one account
            // can drastically reduce computation time
            List<StakerModel> cachedStakerTypes = stakerTypes.FindAll(x => x.Account == account);
            
            // Although there are only ~80 entries in this table at time of development, this could grow so 
            // caching will prevent unnecessary lookups. 
            // CheckForChilled can be called at least 1 and at most 92 times per account
            // So although small, this is a worthwhile optimization
            List<ChilledModel> cachedChilledEvents = chilledEvents.FindAll(c => c.Account == account);
            
            foreach (var balanceChange in accountBalanceChanges)
            {
                var eraIndexStart = GetEraFromBlock(erasInCycle, balanceChange.StartBlock).EraIndex;
                var eraIndexEnd = GetEraFromBlock(erasInCycle, balanceChange.EndBlock).EraIndex;
                var stakerTypeAtStart = GetStakerType(account, cachedStakerTypes, eraIndexStart);

                // The start and end era are the same for this balance range, so no need to do any further edge case handling
                if (eraIndexStart == eraIndexEnd)
                {
                    stakerTypeAtStart = CheckForChilled(cachedChilledEvents, stakerTypeAtStart, account, balanceChange.StartBlock, balanceChange.EndBlock);
                    // Update Bonded staker type for this balance change
                    balanceChange.Bonded.StakerType = stakerTypeAtStart;
                    splitBalanceChanges.Add(balanceChange);
                    continue;
                }

                // Check all eras in between the start and end era, if there are any staker type changes due to chilled events
                // or staker type changes, we need to split the balance change into multiple balance changes
                var newBalanceChanges = GetBalanceChangesBasedOnStakerType(balanceChange, stakerTypeAtStart, eraIndexStart, eraIndexEnd, cachedStakerTypes, erasInCycle, cachedChilledEvents);
                splitBalanceChanges.AddRange(newBalanceChanges);
            }
            
            finalBalanceChanges[account] = splitBalanceChanges;
        }

        return finalBalanceChanges;
    }
    
    // Check if the account is chilled at any point in the block range,
    // if it is, return StakerType.Staker
    // If it is not, return the original staker type
    private string CheckForChilled(List<ChilledModel> chilledEvents, string stakerType, string account, int startBlock, int endBlock)
    {
        // If the staker type is already staker, no need to do anything
        if (stakerType == StakerType.Staker) return StakerType.Staker;
        if (chilledEvents.Count == 0) return stakerType;
        var hasEvent = chilledEvents.Any(c => c.Account == account && c.BlockNumber >= startBlock && c.BlockNumber <= endBlock);
        return hasEvent ? StakerType.Staker : stakerType;
    }
    
    // If the staker type at the start and the end of the range is different, 
    // we need to split the balance change into two separate balance changes
    // This can be done by iterating through each era and determining where the staker type changes
    // For each change in staker type, we can split the balance change into two separate balance changes
    private List<BalanceChangeModel> GetBalanceChangesBasedOnStakerType(
        BalanceChangeModel balanceChangeModel,
        string stakerTypeAtStart,
        int eraIndexStart,
        int eraIndexEnd,
        List<StakerModel> stakerTypes,
        List<EraModel> eras,
        List<ChilledModel> chilledEvents
    )
    {
        var balanceChanges = new List<BalanceChangeModel>();
        // Check to see if the start staker type is chilled. We don't iterate through the first era
        EraModel startEra = GetEraFromEraIndex(eras, eraIndexStart);
        var currentStakerType = CheckForChilled(chilledEvents, stakerTypeAtStart, balanceChangeModel.Account, startEra.StartBlock, startEra.EndBlock);
        // Update bonded staker type for this balance change
        var firstBondedBalanceDetail = balanceChangeModel.Bonded;
        firstBondedBalanceDetail.StakerType = currentStakerType;
        // Set the inital BalanceChangeModel for the first section
        // Note the endBlock will be changed later
        BalanceChangeModel currentNewBalanceChange = new BalanceChangeModel
        {
            Account = balanceChangeModel.Account,
            // Bonded and unlocking should be the same to represent the change from the previous range
            Bonded = firstBondedBalanceDetail,
            Unlocking = balanceChangeModel.Unlocking,
            StartBlock = balanceChangeModel.StartBlock,
        };
        
        // Iterate through each era and split every time the staker type changes
        // Max loop length is 89 based off of cycle duration
        // Note this method must iterate through the entire range as the staker type could change more than one time
        // It skips the first era as we have already checked it
        for (var eraIndex = eraIndexStart + 1; eraIndex <= eraIndexEnd; eraIndex++)
        {
            EraModel era = GetEraFromEraIndex(eras, eraIndex);
            var stakerType = GetStakerType(balanceChangeModel.Account, stakerTypes, eraIndex);
            stakerType = CheckForChilled(chilledEvents, stakerType, balanceChangeModel.Account, era.StartBlock, era.EndBlock);
        
            // No need to split, we have the same stakerType
            if (stakerType == currentStakerType) continue;
            
            // We had a change, insert a new BalanceChangeModel for this range
            // Set the end block of the current balance change to the start block of the new era and add to the list
            currentNewBalanceChange.EndBlock = era.StartBlock - 1;
            // currentNewBalanceChange.DebugLog();
            balanceChanges.Add(currentNewBalanceChange);
            // Create a new balance change model from this block onwards
            var bondedBalanceDetail = new BalanceChangeDetail (
                currentNewBalanceChange.Bonded.BalanceInBlockRange,
                BigInteger.Zero, // Balance doesn't change after first one
                currentNewBalanceChange.Bonded.BalanceInBlockRange,
                stakerType
            );
            var unlockingBalanceDetail = new BalanceChangeDetail (
                currentNewBalanceChange.Unlocking.BalanceInBlockRange,
                BigInteger.Zero, // Balance doesn't change after first one
                currentNewBalanceChange.Unlocking.BalanceInBlockRange
            );
            currentNewBalanceChange = new BalanceChangeModel
            {
                Account = balanceChangeModel.Account,
                Bonded = bondedBalanceDetail,
                Unlocking = unlockingBalanceDetail,
                StartBlock = era.StartBlock,
                // EndBlock is set later
            };
            currentStakerType = stakerType;
        }
        
        // For the last balance change model, set the end block to the original balance change model end block
        currentNewBalanceChange.EndBlock = balanceChangeModel.EndBlock;
        // currentNewBalanceChange.DebugLog();
        balanceChanges.Add(currentNewBalanceChange);
        
        return balanceChanges;
    }
    
    // Get the stake type of the account at a specified era. This will be used to determine the reward rate
    // When resolving for EffectiveBalance
    private string GetStakerType(string account, List<StakerModel> stakerTypes, int eraIndex)
    {
        var stakerModel = stakerTypes.Find(x => x.Account == account && x.EraIndex == eraIndex);
        var stakerType = stakerModel != null ? stakerModel.Type : StakerType.Staker;
        return stakerType;
    }
    
    // Get the era from blockNumber.
    private EraModel GetEraFromBlock(List<EraModel> eras, int blockNumber)
    {
        var era = eras.Find(e => e.StartBlock <= blockNumber && e.EndBlock >= blockNumber && e.EndBlock != -1);
        if (era == null) throw new Exception("Era not found for start block: " + blockNumber);
        // find the era with the earliest start block
        // var eraIndex = eras.Find(e => e.StartBlock == era.StartBlock);
        return era;
    }
    
    // Get the eraModel from eraIndex.
    private EraModel GetEraFromEraIndex(List<EraModel> eras, int eraIndex)
    {
        var era = eras.Find(e => e.EraIndex == eraIndex);
        if (era == null) throw new Exception("Era not found for index: " + eraIndex);
        return era;
    }
}