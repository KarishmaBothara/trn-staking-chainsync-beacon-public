using GraphQL;
using GraphQL.Client.Abstractions;
using MatrixEngine.Core.GraphQL.ActiveEras;
using MatrixEngine.Core.Testing.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatrixEngine.Core.Testing.GraphQL;

public class GetActiveErasConnectionTests
{
    private readonly GetActiveErasConnection _getActiveErasConnection;
    private readonly Mock<ILogger<GetActiveErasConnection>> _logger;
    private readonly Mock<IGraphQLClient> _client;

    public GetActiveErasConnectionTests()
    {
        _logger = new Mock<ILogger<GetActiveErasConnection>>();
        _client = new Mock<IGraphQLClient>();
        _getActiveErasConnection = new GetActiveErasConnection(_client.Object, _logger.Object);
    }

    [Fact]
    public async Task FetchEras_WhenApiReturnsEras()
    {
        _client.Setup(m =>
            m.SendQueryAsync<GetActiveErasConnectionResponseType>(
                It.Is<GraphQLRequest>(r => 
                    r.Variables.GetType().GetProperty("after").GetValue(r.Variables, null) == null),
                CancellationToken.None)).ReturnsAsync(GraphQLFixture.LoadEras(1));
        _client.Setup(m =>
            m.SendQueryAsync<GetActiveErasConnectionResponseType>(
                It.Is<GraphQLRequest>(r => 
                    (string)r.Variables.GetType().GetProperty("after").GetValue(r.Variables, null) == "10"),
                CancellationToken.None)).ReturnsAsync(GraphQLFixture.LoadEras(2));

        var result = await _getActiveErasConnection.FetchActiveEras();

        Assert.Equal(20, result.Count);
    }
}