using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using MatrixEngine.GraphQL.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatrixEngine.Core.GraphQL.Withdrawns;

public interface IGetWithdrawnsConnection
{
    Task<List<WithdrawnNodeType>> FetchWithdrawns(int startBlock, int endBlock);
}

public class GetWithdrawnsConnection : IGetWithdrawnsConnection
{
    private ILogger<GetWithdrawnsConnection> _logger;
    private IGraphQLClient _client;
    private const int FirstFetchBatch = 500;

    private const string Query = @"
      query GetWithdrawnsConnection($first: Int!, $after: String, $startBlock: Int!, $endBlock: Int!){
        withdrawnsConnection(
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

    public GetWithdrawnsConnection(IGraphQLClient client, ILogger<GetWithdrawnsConnection> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<List<WithdrawnNodeType>> FetchWithdrawns(int startBlock, int endBlock)
    {
        string? after = null;
        var withdrawns = new List<WithdrawnNodeType>();
        bool hasNextPage;

        do
        {
            var request = new GraphQLRequest
            {
                Query = Query,
                OperationName = "GetWithdrawnsConnection",
                Variables = new
                {
                    first = FirstFetchBatch,
                    after,
                    startBlock,
                    endBlock
                }
            };

            var response = await _client.SendQueryAsync<GetWithdrawnsConnectionResponseType>(request);

            var connection = response.Data.WithdrawnsConnection;
            withdrawns.AddRange(connection.Edges.Select(e => e.Node));
            hasNextPage = connection.PageInfo.HasNextPage;
            after = connection.PageInfo.EndCursor;
        } while (hasNextPage);

        return withdrawns;
    }
}