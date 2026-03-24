using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.GraphQL.Chilled;

public interface IGetChilledConnection
{
    Task<List<ChilledType>> FetchChilledEvents();
}

public class GetChilledConnection : IGetChilledConnection
{
    private const int FirstFetchBatch = 500;
    private readonly ILogger<GetChilledConnection> _logger;
    private readonly IGraphQLClient _client;

    private const string Query = @"
       query GetChilledConnection($first: Int!, $after: String) {
        chilledsConnection(first: $first, after: $after, orderBy: [blockNumber_ASC]) {
          edges {
              node {
                blockNumber
                stash
              } 
          }
          pageInfo {
            hasNextPage
            hasPreviousPage
            startCursor
            endCursor
          } 
          totalCount
        }
      }";

    public GetChilledConnection(IGraphQLClient client, ILogger<GetChilledConnection> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<List<ChilledType>> FetchChilledEvents()
    {
        _logger.LogInformation("Starting to fetch chilled events");
        string? after = null;
        var chilledEvents = new List<ChilledType>();
        try
        {
            bool hasNextPage;
            do
            {
                var request = new GraphQLRequest
                {
                    Query = Query,
                    OperationName = "GetChilledConnection",
                    Variables = new
                    {
                        first = FirstFetchBatch,
                        after,
                    }
                };
                var response = await _client.SendQueryAsync<GetChilledConnectionResponseType>(request);
                var connection = response.Data.ChilledsConnection;
                chilledEvents.AddRange(connection.Edges.Select(e => e.Node));

                hasNextPage = connection.PageInfo.HasNextPage;
                after = connection.PageInfo.EndCursor;
            } while (hasNextPage);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception("Error fetching chilled events", e);
        }

        return chilledEvents;
    }
} 