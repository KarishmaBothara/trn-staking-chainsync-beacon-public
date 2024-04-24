using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using MatrixEngine.GraphQL.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatrixEngine.Core.GraphQL.ActiveEras;

public interface IGetActiveErasConnection
{
    Task<List<ActiveEraType>> FetchActiveEras();
}

public class GetActiveErasConnection : IGetActiveErasConnection
{
    private const int FirstFetchBatch = 500;

    private ILogger<GetActiveErasConnection> _logger;
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
        string? after = null;
        bool hasNextPage;
        var activeEras = new List<ActiveEraType>();

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

        return activeEras;
    }
}