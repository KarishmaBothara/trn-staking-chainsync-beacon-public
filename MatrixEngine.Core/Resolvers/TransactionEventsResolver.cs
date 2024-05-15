using MatrixEngine.Core.GraphQL.Bondeds;
using MatrixEngine.Core.GraphQL.Withdrawns;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.Events;
using MatrixEngine.Core.Services;

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
    private readonly IGetBondedsCnnection _getBondedsCnnection;
    private readonly IEraService _eraService;
    private IAccountPunishmentMarkService _accountPunishmentMarkService;


    public TransactionEventsResolver(IGetBondedsCnnection getBondedsCnnection,
        IGetWithdrawnsConnection getWithdrawnsConnection, ITransactionEventService transactionEventService,
        IEraService eraService, IAccountPunishmentMarkService accountPunishmentMarkService)
    {
        _accountPunishmentMarkService = accountPunishmentMarkService;
        _eraService = eraService;
        _getBondedsCnnection = getBondedsCnnection;
        _getWithdrawnsConnection = getWithdrawnsConnection;
        _transactionEventService = transactionEventService;
    }

    public async Task Resolve()
    {
        var startBlock = await _transactionEventService.GetLatestBlockNumber();
        var latestEra = await _eraService.GetLatestFinishedEra();
        var endBlock = latestEra.EndBlock;

        await FetchEventsInBlockRange(startBlock, endBlock);
    }

    public async Task FetchEventsInBlockRange(int startBlock, int endBlock)
    {
        var bondedTypes = await _getBondedsCnnection.FetchBondeds(startBlock, endBlock);
        var withdrawnTypes = await _getWithdrawnsConnection.FetchWithdrawns(startBlock, endBlock);
        //map each one to TransactionModel
        var bondedEvents = bondedTypes.Select(b => new TransactionModel()
        {
            Account = b.Stash,
            Amount = b.Amount,
            BlockNumber = b.BlockNumber,
            Type = TransactionType.Bonded,
        });
        var withdrawnEvents = withdrawnTypes.Select(w => new TransactionModel()
        {
            Account = w.Stash,
            Amount = w.Amount,
            BlockNumber = w.BlockNumber,
            Type = TransactionType.Withdrawn
        });
        //merge bondedEvents and withdrawnEvents and sort
        var transactionEvents = bondedEvents.Concat(withdrawnEvents).OrderBy(x => x.BlockNumber).ToList();
        //save transaction events
        await _transactionEventService.UpsertTransactionEvents(transactionEvents);

        var accountPunishmentMarks = withdrawnEvents.Select(m => new AccountPunishmentMarkModel()
        {
            Account = m.Account,
            BlockNumber = m.BlockNumber,
            Type = m.Type,
            Amount = m.Amount
        }).ToList();

        await _accountPunishmentMarkService.UpsertAccountPunishmentMarks(accountPunishmentMarks);
    }
}