using MatrixEngine.Core.Engine;
using MatrixEngine.Core.GraphQL.Bondeds;
using MatrixEngine.Core.GraphQL.Withdrawns;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.Events;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Testing.Fixtures;
using Moq;

namespace MatrixEngine.Core.Testing.Resolvers;

public class TransactionEventsResolverTests
{
    private readonly Mock<IGetBondedsCnnection> _getBondedsConnection;
    private readonly Mock<IGetWithdrawnsConnection> _getWithdrawnsConnection;
    private readonly Mock<ITransactionEventService> _transactionEventService;
    private readonly Mock<IEraService> _eraService;
    private readonly Mock<IAccountPunishmentMarkService> _accountPunishmentService;

    public TransactionEventsResolverTests()
    {
        _getBondedsConnection = new Mock<IGetBondedsCnnection>();
        _getWithdrawnsConnection = new Mock<IGetWithdrawnsConnection>();
        _transactionEventService = new Mock<ITransactionEventService>();
        _eraService = new Mock<IEraService>();
        _accountPunishmentService = new Mock<IAccountPunishmentMarkService>();
    }

    [Fact]
    public async Task Resolve_WhenApiReturns()
    {
        var mockBondedTypes = GraphQLFixture.LoadBondeds(1);
        var mockWithdrawnTypes = GraphQLFixture.LoadWithdrawns(1);
        var bondedTypes = mockBondedTypes.Data.BondedsConnection.Edges.Select(b => b.Node).ToList();
        var withdrawnTypes = mockWithdrawnTypes.Data.WithdrawnsConnection.Edges.Select(w => w.Node).ToList();

        _getBondedsConnection.Setup(m => m.FetchBondeds(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(bondedTypes);
        _getWithdrawnsConnection.Setup(m => m.FetchWithdrawns(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(withdrawnTypes);

        var transactionEventsResolver = new TransactionEventsResolver(_getBondedsConnection.Object,
            _getWithdrawnsConnection.Object, _transactionEventService.Object, _eraService.Object,
            _accountPunishmentService.Object);
        await transactionEventsResolver.FetchEventsInBlockRange(1, 2);

        _getBondedsConnection.Verify(m => m.FetchBondeds(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        _getWithdrawnsConnection.Verify(m => m.FetchWithdrawns(It.IsAny<int>(), It.IsAny<int>()), Times.Once);

        _transactionEventService.Verify(
            m => m.UpsertTransactionEvents(
                It.Is<List<TransactionModel>>(
                    list =>
                        list.Count == 20
                        && list.Select(x => x.Type == TransactionType.Bonded).Count() == 20
                        && list.Select(x => x.Type == TransactionType.Withdrawn).Count() == 20
                        && list[0].BlockNumber < list[list.Count - 1].BlockNumber
                )));

        _accountPunishmentService.Verify(m =>
            m.UpsertAccountPunishmentMarks(
                It.Is<List<AccountPunishmentMarkModel>>(
                    l => l.Any(a => a.Type == TransactionType.Withdrawn))));
    }
}