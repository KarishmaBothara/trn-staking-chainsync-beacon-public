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
        int pageCount = 0;

        try
        {
            do
            {
                pageCount++;
                _logger.LogInformation("Fetching stakers page {PageCount} with cursor {Cursor}", pageCount, after ?? "null");

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
                
                
                if (response?.Data?.StakersConnection == null)
                {
                    _logger.LogWarning("Received null response from GraphQL server");
                    break;
                }
                
                var connection = response.Data.StakersConnection;
                var newItems = connection.Edges.Select(e => e.Node).ToList();
                _logger.LogInformation("Fetched {Count} stakers in this page", newItems.Count);
                stakers.AddRange(newItems);
                hasNextPage = connection.PageInfo.HasNextPage;
                after = connection.PageInfo.EndCursor;
                
                // If we got zero items but hasNextPage is true, something is wrong
                if (newItems.Count == 0 && hasNextPage)
                {
                    _logger.LogWarning("Received 0 items but hasNextPage is true, breaking pagination loop");
                    break;
                }
            } while (hasNextPage);

            _logger.LogInformation("Completed fetching all stakers for era {EraIndex}. Total count: {TotalCount}", eraIndex, stakers.Count);
            return stakers;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception("Error fetching stakers ", e);
        }
    }
}