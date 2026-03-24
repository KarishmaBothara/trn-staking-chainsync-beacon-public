using MatrixEngine.Core.GraphQL.Bondeds;
using MatrixEngine.Core.GraphQL.Slashed;
using MatrixEngine.Core.GraphQL.Unbondeds;
using MatrixEngine.Core.GraphQL.Withdrawns;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.Events;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Substrate.Ledger;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.Resolvers;

public interface ITransactionEventsResolver
{
    Task FetchEventsInBlockRange(int startBlock, int endBlock);
    Task Resolve();
}

public class TransactionEventsResolver : ITransactionEventsResolver
{
    private readonly ITransactionEventService _transactionEventService;
    private readonly IGetWithdrawnsConnection _getWithdrawnsConnection;
    private readonly IGetBondedsConnection _getBondedsConnection;
    private readonly IGetUnbondedsConnection _getUnBondedsConnection;
    private readonly IGetSlashedsConnection _getSlashedConnection;
    private readonly ISubstrateLedgerClient _substrateLedgerClient;
    private readonly IStakerService _stakerService;
    private readonly IEraService _eraService;
    private readonly ILogger<TransactionEventsResolver> _logger;


    public TransactionEventsResolver(
        IGetBondedsConnection getBondedsCnnection,
        IGetWithdrawnsConnection getWithdrawnsConnection,
        IGetUnbondedsConnection getUnBondedsConnection,
        IGetSlashedsConnection getSlashedConnection,
        ITransactionEventService transactionEventService,
        ISubstrateLedgerClient substrateLedgerClient,
        IStakerService stakerService,
        IEraService eraService,
        ILogger<TransactionEventsResolver> logger
    )
    {
        _eraService = eraService;
        _getBondedsConnection = getBondedsCnnection;
        _getUnBondedsConnection = getUnBondedsConnection;
        _getWithdrawnsConnection = getWithdrawnsConnection;
        _getSlashedConnection = getSlashedConnection;
        _transactionEventService = transactionEventService;
        _stakerService = stakerService;
        _substrateLedgerClient = substrateLedgerClient;
        _logger = logger;
    }

    public async Task Resolve()
    {
        var startBlock = await _transactionEventService.GetLatestBlockNumber();
        var latestEra = await _eraService.GetLatestFinishedEra();
        if (latestEra == null) throw new Exception("Can't find latest finished era");
        var eraEndBlock = latestEra.EndBlock;
        var blockWindow = 1000;
        var currentStartBlock = startBlock;
        var currentEndBlock = startBlock + blockWindow;

        // On mainnet, there are very few events in the first 7 million blocks,
        // Let's speed up the process and check the first 7 million in one go, then check every 1000 after
        if (startBlock < 7000000 && eraEndBlock > 7000000)
        {
            currentEndBlock = 7000000;
        }

        // Fetch blockWindow blocks at a time until we reach the latest era. 
        while (currentStartBlock < eraEndBlock)
        {
            await FetchEventsInBlockRange(currentStartBlock, currentEndBlock);
            currentStartBlock = currentEndBlock + 1;
            currentEndBlock = currentStartBlock + blockWindow;
        }
    }

    public async Task FetchEventsInBlockRange(int startBlock, int endBlock)
    {
        _logger.LogInformation($"Fetching Bonded, Withdrawn, Unbonded and Slashed events in block range: {startBlock} - {endBlock}");
        const int maxRetries = 20;
        const int retryDelayMs = 30000; // 30 seconds
        int retryCount = 0;

        while (true)
        {
            try
            {
                var bondedTypes = await _getBondedsConnection.FetchBondeds(startBlock, endBlock);
                var withdrawnTypes = await _getWithdrawnsConnection.FetchWithdrawns(startBlock, endBlock);
                var unbondedTypes = await _getUnBondedsConnection.FetchUnbondeds(startBlock, endBlock);
                var slashedTypes = await _getSlashedConnection.FetchSlashed(startBlock, endBlock);
                
                // map each one to TransactionModel
                var bondedEvents = await CategoriseBondedEventsAsync(bondedTypes);
                // If at the genesis block, insert one bonded event per validator to represent their initial staking balance
                if (startBlock == 0)
                {
                    var genesisBalances = await FetchGenesisBalancesAsync();
                    bondedEvents = bondedEvents.Concat(genesisBalances);
                }
                var withdrawnEvents = withdrawnTypes.Select(w => new TransactionModel()
                {
                    Account = w.Stash,
                    Amount = w.Amount,
                    BlockNumber = w.BlockNumber,
                    Type = TransactionType.Withdrawn
                });
                var unbondedEvents = unbondedTypes.Select(u => new TransactionModel()
                {
                    Account = u.Stash,
                    Amount = u.Amount,
                    BlockNumber = u.BlockNumber,
                    Type = TransactionType.Unbonded
                });
                var slashedEvents = slashedTypes.Select(s => new TransactionModel()
                {
                    Account = s.Staker,
                    Amount = s.Amount,
                    BlockNumber = s.BlockNumber,
                    Type = TransactionType.Slashed
                });
                // merge bondedEvents, unbondedEvents, withdrawnEvents and slashedEvents and sort
                var transactionEvents = bondedEvents
                    .Concat(withdrawnEvents)
                    .Concat(unbondedEvents)
                    .Concat(slashedEvents)
                    .OrderBy(x => x.BlockNumber)
                    .ToList();
                
                //save transaction events
                await _transactionEventService.UpsertTransactionEvents(transactionEvents);
                break; // Success - exit the retry loop
            }
            catch (Exception e)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    _logger.LogError(e, $"Error fetching events after {retryCount} retries. Giving up.");
                    throw new Exception("Error fetching Transaction Events: ", e);
                }
                _logger.LogWarning(e, $"Error fetching events. Retry {retryCount}/{maxRetries} in {retryDelayMs}ms");
                await Task.Delay(retryDelayMs);
            }
        }
    }
    
    // Determine whether each event is a bonded or rebonded event
    public async Task<IEnumerable<TransactionModel>> CategoriseBondedEventsAsync(List<BondedNodeType> bondedTypes)
    {
        _logger.LogInformation($"Starting categorising bonded events. Count: {bondedTypes.Count}");
        var bondedEvents = new List<TransactionModel>(bondedTypes.Count);
        foreach (BondedNodeType bondedNodeType in bondedTypes)
        {
            string transactionType = await _substrateLedgerClient.GetBondedTypeAsync(bondedNodeType.Stash, bondedNodeType.BlockNumber, bondedNodeType.Amount);

            bondedEvents.Add(new TransactionModel()
            {
                Account = bondedNodeType.Stash,
                Amount = bondedNodeType.Amount,
                BlockNumber = bondedNodeType.BlockNumber,
                Type = transactionType
            });
        }
        return bondedEvents.ToList(); ;
    }
    
    // For the initial validators, their balances are locked without an event. 
    // This function will fetch the genesis validators from the stakers table, and create a fake bonded event
    // for each validator at block 0.
    public async Task<IEnumerable<TransactionModel>> FetchGenesisBalancesAsync()
    {
        _logger.LogInformation($"Starting calculate genesis balance snapshot");
        // fetch genesis validator balance
        List<StakerModel> genesisValidators = await _stakerService.GetAllStakerTypesByEraIndex(0);

        var events = genesisValidators.Select(staker =>
            new TransactionModel()
            {
                Account = staker.Account,
                Amount = staker.TotalStake,
                BlockNumber = 0,
                Type = TransactionType.Bonded,
            }
        );
        return events.ToList();
    }

}