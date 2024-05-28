using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.GraphQL.ActiveEras;

public interface IGetActiveErasConnection
{
    Task<List<ActiveEraType>> FetchActiveEras();
}

public class GetActiveErasConnection : IGetActiveErasConnection
{
    private const int FirstFetchBatch = 500;

    private readonly ILogger<GetActiveErasConnection> _logger;
    private readonly IGraphQLClient _client;

    private const string Query = @"
       query GetActiveErasConnection($first: Int!, $after: String) {
        activeErasConnection(first: $first, after: $after, orderBy: [eraIndex_ASC]) {
          edges {
              node {
                eraIndex
                blockNumber
                timestamp
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


    public GetActiveErasConnection(IGraphQLClient client, ILogger<GetActiveErasConnection> logger)
    {
        _client = client;
        _logger = logger;
    }


    public async Task<List<ActiveEraType>> FetchActiveEras()
    {
        _logger.LogInformation("Starting to fetch active eras");
        string? after = null;
        var activeEras = new List<ActiveEraType>();
        try
        {
            bool hasNextPage;
            do
            {
                var request = new GraphQLRequest
                {
                    Query = Query,
                    OperationName = "GetActiveErasConnection",
                    Variables = new
                    {
                        first = FirstFetchBatch, after,
                    }
                };
                var response = await _client.SendQueryAsync<GetActiveErasConnectionResponseType>(request);
                var connection = response.Data.ActiveErasConnection;
                activeEras.AddRange(connection.Edges.Select(e => e.Node));

                hasNextPage = connection.PageInfo.HasNextPage;
                after = connection.PageInfo.EndCursor;
            } while (hasNextPage);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return new List<ActiveEraType>();
        }

        return activeEras;
    }
}