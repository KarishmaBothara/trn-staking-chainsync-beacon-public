using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using MatrixEngine.GraphQL.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatrixEngine.Core.GraphQL.Bondeds;

public interface IGetBondedsCnnection
{
    Task<List<BondedNodeType>> FetchBondeds(int startBlock, int endBlock);
}

public class GetBondedsCnnection : IGetBondedsCnnection
{
    private ILogger<GetBondedsCnnection> _logger;
    private IGraphQLClient _client;

    private const int FirstFetchBatch = 500;

    private const string Query = @"
        query GetBondedConnection($first: Int!, $after: String, $startBlock: Int!, $endBlock: Int!){
          bondedsConnection(
            first: $first
            after: $after
            where: { blockNumber_gte: $startBlock, blockNumber_lt: $endBlock },

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
        string? after = null;
        var bondeds = new List<BondedNodeType>();
        bool hasNextPage;

        do
        {
            var request = new GraphQLRequest
            {
                Query = Query,
                OperationName = "GetBondedsCnnection",
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
}