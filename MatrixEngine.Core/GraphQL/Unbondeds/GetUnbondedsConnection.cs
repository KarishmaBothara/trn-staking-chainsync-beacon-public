using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatrixEngine.Core.GraphQL.Unbondeds;

public interface IGetUnbondedsConnection
{
    Task<List<UnbondedNodeType>> FetchUnbondeds(int startBlock, int endBlock);
}

public class GetUnbondedsConnection : IGetUnbondedsConnection
{
    private readonly ILogger<GetUnbondedsConnection> _logger;
    private readonly IGraphQLClient _client;

    private const int FirstFetchBatch = 500;

    private const string Query = @"
        query GetUnbondedsConnection($first: Int!, $after: String, $startBlock: Int!, $endBlock: Int!){
          unbondedsConnection(
            first: $first
            after: $after
            where: { blockNumber_gte: $startBlock, blockNumber_lte: $endBlock },
            orderBy: [blockNumber_ASC]) {
              edges {
                node {
                  blockNumber
                  stash
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

    public GetUnbondedsConnection(IGraphQLClient client, ILogger<GetUnbondedsConnection> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<List<UnbondedNodeType>> FetchUnbondeds(int startBlock, int endBlock)
    {
        _logger.LogInformation("Starting to fetch unbonded events");
        string? after = null;
        var unbondeds = new List<UnbondedNodeType>();

        try
        {
            var hasNextPage = false;
            do
            {
                var request = new GraphQLRequest
                {
                    Query = Query,
                    OperationName = "GetUnbondedsConnection",
                    Variables = new
                    {
                        first = FirstFetchBatch,
                        after,
                        startBlock,
                        endBlock,
                    }
                };

                var response = await _client.SendQueryAsync<GetUnbondedsConnectionResponseType>(request);
                var connection = response.Data.UnbondedsConnection;
                hasNextPage = connection.PageInfo.HasNextPage;

                unbondeds.AddRange(connection.Edges.Select(e => e.Node));

                after = connection.PageInfo.EndCursor;
            } while (hasNextPage);

            return unbondeds;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception("Error fetching unbonded events", e);
        }
    }
} 