using GraphQL;
using GraphQL.Client.Abstractions;
using MatrixEngine.Core.GraphQL.Withdrawns;
using MatrixEngine.Core.Testing.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatrixEngine.Core.Testing.GraphQL;

public class GetWithdrawnsConnectionTests
{
    private Mock<IGraphQLClient> _client;
    private Mock<ILogger<GetWithdrawnsConnection>> _logger;
    private GetWithdrawnsConnection _getWithdrawnsConnection;

    public GetWithdrawnsConnectionTests()
    {
        _client = new Mock<IGraphQLClient>();
        _logger = new Mock<ILogger<GetWithdrawnsConnection>>();

        _getWithdrawnsConnection = new GetWithdrawnsConnection(_client.Object, _logger.Object);
    }

    [Fact]
    public async Task FetchWithdrawns_WhenApiReturns()
    {
        _client.Setup(m =>
            m.SendQueryAsync<GetWithdrawnsConnectionResponseType>(
                It.Is<GraphQLRequest>(r => 
                    r.Variables.GetType().GetProperty("after").GetValue(r.Variables, null) == null),
                CancellationToken.None)).ReturnsAsync(GraphQLFixture.LoadWithdrawns(1));
        _client.Setup(m =>
            m.SendQueryAsync<GetWithdrawnsConnectionResponseType>(
                It.Is<GraphQLRequest>(r => 
                    (string)r.Variables.GetType().GetProperty("after").GetValue(r.Variables, null) == "10"),
                CancellationToken.None)).ReturnsAsync(GraphQLFixture.LoadWithdrawns(2));

        var result = await _getWithdrawnsConnection.FetchWithdrawns(1, 2);

        Assert.Equal(20, result.Count);
    }
}