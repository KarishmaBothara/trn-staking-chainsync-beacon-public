using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatrixEngine.Core.GraphQL.Slashed;

public interface IGetSlashedsConnection
{
    Task<List<SlashedNodeType>> FetchSlashed(int startBlock, int endBlock);
}

public class GetSlashedsConnection : IGetSlashedsConnection
{
    private readonly ILogger<GetSlashedsConnection> _logger;
    private readonly IGraphQLClient _client;

    private const int FirstFetchBatch = 500;

    private const string Query = @"
        query GetSlashedsConnection($first: Int!, $after: String, $startBlock: Int!, $endBlock: Int!){
          slashedsConnection(
            first: $first
            after: $after
            where: { blockNumber_gte: $startBlock, blockNumber_lte: $endBlock },

            orderBy: [blockNumber_ASC]) {
              edges {
                node {
                  blockNumber
                  staker
                  amount
                }
              }
                pageInfo {
                hasNextPage
                startCursor
                endCursor
              }
              totalCount
            }
        }";

    public GetSlashedsConnection(IGraphQLClient client, ILogger<GetSlashedsConnection> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<List<SlashedNodeType>> FetchSlashed(int startBlock, int endBlock)
    {
        _logger.LogInformation("Starting to fetch slashed events");
        string? after = null;
        var slashed = new List<SlashedNodeType>();

        try
        {
            var hasNextPage = false;
            do
            {
                var request = new GraphQLRequest
                {
                    Query = Query,
                    OperationName = "GetSlashedsConnection",
                    Variables = new
                    {
                        first = FirstFetchBatch,
                        after,
                        startBlock,
                        endBlock,
                    }
                };

                var response = await _client.SendQueryAsync<GetSlashedsConnectionResponseType>(request);
                var connection = response.Data.SlashedsConnection;
                hasNextPage = connection.PageInfo.HasNextPage;
                var nodes = connection.Edges.Select(e => e.Node).ToList();
                slashed.AddRange(connection.Edges.Select(e => e.Node));
                after = connection.PageInfo.EndCursor;
            } while (hasNextPage);

            return slashed;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception("Error fetching slasheds", e);
        }
    }
} 