using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatrixEngine.Core.GraphQL.Bondeds;

public interface IGetBondedsConnection
{
    Task<List<BondedNodeType>> FetchBondeds(int startBlock, int endBlock);
}

public class GetBondedsCnnection : IGetBondedsConnection
{
    private readonly ILogger<GetBondedsCnnection> _logger;
    private readonly IGraphQLClient _client;

    private const int FirstFetchBatch = 500;

    private const string Query = @"
        query GetBondedsConnection($first: Int!, $after: String, $startBlock: Int!, $endBlock: Int!){
          bondedsConnection(
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

    public GetBondedsCnnection(IGraphQLClient client, ILogger<GetBondedsCnnection> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<List<BondedNodeType>> FetchBondeds(int startBlock, int endBlock)
    {
        _logger.LogInformation("Starting to fetch bonded events");
        string? after = null;
        var bondeds = new List<BondedNodeType>();

        try
        {
            var hasNextPage = false;
            do
            {
                var request = new GraphQLRequest
                {
                    Query = Query,
                    OperationName = "GetBondedsConnection",
                    Variables = new
                    {
                        first = FirstFetchBatch,
                        after,
                        startBlock,
                        endBlock,
                    }
                };

                var response = await _client.SendQueryAsync<GetBondedsConnectionResponseType>(request);
                var connection = response.Data.BondedsConnection;
                hasNextPage = connection.PageInfo.HasNextPage;

                bondeds.AddRange(connection.Edges.Select(e => e.Node));

                after = connection.PageInfo.EndCursor;
            } while (hasNextPage);

            return bondeds;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception("Error fetching bonded events", e);
        }
    }
}