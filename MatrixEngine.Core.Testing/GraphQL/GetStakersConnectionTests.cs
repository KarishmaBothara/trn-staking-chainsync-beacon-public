using GraphQL;
using GraphQL.Client.Abstractions;
using MatrixEngine.Core.GraphQL.Stakers;
using MatrixEngine.Core.Testing.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatrixEngine.Core.Testing.GraphQL;

public class GetStakersConnectionTests
{
    private readonly Mock<IGraphQLClient> _client;
    private readonly Mock<ILogger<GetStakersConnection>> _logger;
    private readonly GetStakersConnection _getStakersConnection;

    public GetStakersConnectionTests()
    {
        _client = new Mock<IGraphQLClient>();
        _logger = new Mock<ILogger<GetStakersConnection>>();
        
        _getStakersConnection = new GetStakersConnection(_client.Object, _logger.Object);
    }

    [Fact]
    public async Task FetchStakers_WhenApiReturns()
    {
        _client.Setup(m =>
            m.SendQueryAsync<GetStakersConnectionResponseType>(
                It.Is<GraphQLRequest>(r => 
                    r.Variables.GetType().GetProperty("after").GetValue(r.Variables, null) == null),
                CancellationToken.None)).ReturnsAsync(GraphQLFixture.LoadStakers(1));
        _client.Setup(m =>
            m.SendQueryAsync<GetStakersConnectionResponseType>(
                It.Is<GraphQLRequest>(r => 
                    (string)r.Variables.GetType().GetProperty("after").GetValue(r.Variables, null) == "10"),
                CancellationToken.None)).ReturnsAsync(GraphQLFixture.LoadStakers(2));

        var result = await _getStakersConnection.FetchStakers(1);

        Assert.Equal(20, result.Count);
    }
}