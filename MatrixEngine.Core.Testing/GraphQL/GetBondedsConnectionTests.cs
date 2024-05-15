using GraphQL;
using GraphQL.Client.Abstractions;
using MatrixEngine.Core.GraphQL.Bondeds;
using MatrixEngine.Core.Testing.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatrixEngine.Core.Testing.GraphQL;

public class GetBondedsConnectionTests
{
    private readonly Mock<ILogger<GetBondedsCnnection>> _logger;
    private readonly Mock<IGraphQLClient> _client;
    private readonly GetBondedsCnnection _getBondedsConnection;

    public GetBondedsConnectionTests()
    {
        _logger = new Mock<ILogger<GetBondedsCnnection>>();
        _client = new Mock<IGraphQLClient>();
        _getBondedsConnection = new GetBondedsCnnection(_client.Object, _logger.Object);
    }

    [Fact]
    public async Task FetchBondeds_WhenApiReturns()
    {
        _client.Setup(m =>
            m.SendQueryAsync<GetBondedsConnectionResponseType>(
                It.Is<GraphQLRequest>(r => 
                    r.Variables.GetType().GetProperty("after").GetValue(r.Variables, null) == null),
                CancellationToken.None)).ReturnsAsync(GraphQLFixture.LoadBondeds(1));
        _client.Setup(m =>
            m.SendQueryAsync<GetBondedsConnectionResponseType>(
                It.Is<GraphQLRequest>(r => 
                    (string)r.Variables.GetType().GetProperty("after").GetValue(r.Variables, null) == "10"),
                CancellationToken.None)).ReturnsAsync(GraphQLFixture.LoadBondeds(2));

        var result = await _getBondedsConnection.FetchBondeds(1, 2);

        Assert.Equal(20, result.Count);
    }
}