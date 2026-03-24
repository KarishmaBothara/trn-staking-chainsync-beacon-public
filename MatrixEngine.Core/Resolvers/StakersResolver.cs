using MatrixEngine.Core.GraphQL.Stakers;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.Resolvers;

public interface IStakersResolver
{
    Task FetchStakersByEraIndex(int eraIndex);
    Task Resolve();
}

public class StakersResolver : IStakersResolver
{
    private readonly IGetStakersConnection _getStakersConnection;
    private readonly IStakerService _stakerService;
    private readonly IEraService _eraService;
    private readonly ILogger<StakersResolver> _logger;

    public StakersResolver(IGetStakersConnection getStakersConnection, IStakerService stakerService, IEraService eraService, ILogger<StakersResolver> logger)
    {
        _eraService = eraService;
        _stakerService = stakerService;
        _getStakersConnection = getStakersConnection;
        _logger = logger;
    }
    
    public async Task FetchStakersByEraIndex(int eraIndex)
    {
        const int maxRetries = 10;
        const int retryDelayMs = 30000; // 30 seconds
        int retryCount = 0;

        while (true)
        {
            try
            {
                var stakerTypes = await _getStakersConnection.FetchStakers(eraIndex);
                await _stakerService.ResolveStakersAndSave(stakerTypes);
                break; // Success - exit the retry loop
            }
            catch (Exception e)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    _logger.LogError(e, $"Error fetching stakers for era {eraIndex} after {retryCount} retries. Giving up.");
                    throw new Exception($"Error fetching stakers for era {eraIndex}", e);
                }
                _logger.LogWarning(e, $"Error fetching stakers for era {eraIndex}. Retry {retryCount}/{maxRetries} in {retryDelayMs}ms");
                await Task.Delay(retryDelayMs);
            }
        }
    }

    public async Task Resolve()
    {
        var latestEra = await _eraService.GetLatestFinishedEra();
        if (latestEra == null) throw new Exception("Can't find latest finished era");
        var latestFetchStakerEraIndex = await _stakerService.GetLatestEraFetchedStakerTypes();
        // loop eras that have not fetched stakers.
        // We will also search for the latest fetch staker era index as there is a chance that this was stopped
        // during the upserting operation
        for(var i = latestFetchStakerEraIndex; i <= latestEra.EraIndex; i++)
        {
            await FetchStakersByEraIndex(i);
        }
    }
}