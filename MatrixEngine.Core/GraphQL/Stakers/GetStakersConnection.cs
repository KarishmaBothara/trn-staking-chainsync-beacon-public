using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.GraphQL.Stakers;

public interface IGetStakersConnection
{
    Task<List<StakerNodeType>> FetchStakers(int eraIndex);
}

public class GetStakersConnection : IGetStakersConnection
{
    private readonly ILogger<GetStakersConnection> _logger;
    private readonly IGraphQLClient _client;

    private const int FirstFetchBatch = 500;

    private const string Query = @"
    query GetStakersConnection($first: Int!, $eraIndex: Int!, $after: String) {
      stakersConnection(
        first: $first
        after: $after
        where: { eraIndex_eq: $eraIndex }
        orderBy: [eraIndex_DESC]
      ) {
        edges {
          node {
            stash
            eraIndex
            stakerType
            totalStake
            parentStash
          }
        }
        pageInfo {
          hasNextPage
          startCursor
          endCursor
        }
        totalCount
      }
    }
    ";

    public GetStakersConnection(IGraphQLClient client, ILogger<GetStakersConnection> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<List<StakerNodeType>> FetchStakers(int eraIndex)
    {
        _logger.LogInformation("Starting to fetch stakers types");
        string? after = null;
        var stakers = new List<StakerNodeType>();
        bool hasNextPage;

        try
        {
            do
            {
                var request = new GraphQLRequest
                {
                    Query = Query,
                    OperationName = "GetStakersConnection",
                    Variables = new
                    {
                        first = FirstFetchBatch,
                        eraIndex,
                        after
                    }
                };

                var response = await _client.SendQueryAsync<GetStakersConnectionResponseType>(request);
                var connection = response.Data.StakersConnection;
                stakers.AddRange(connection.Edges.Select(e => e.Node));
                hasNextPage = connection.PageInfo.HasNextPage;
                after = connection.PageInfo.EndCursor;
            } while (hasNextPage);

            return stakers;
        }
        catch (Exception e)
        {
            _logger.LogInformation(e.Message);
            return new List<StakerNodeType>();
        }
    }
}