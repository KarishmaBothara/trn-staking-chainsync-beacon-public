using MatrixEngine.Core.GraphQL.ActiveEras;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.Resolvers;

public interface IErasResolver
{
    Task Resolve();
}

public class ErasResolver : IErasResolver
{
    private readonly IGetActiveErasConnection _getActiveErasConnection;
    private readonly IEraService _eraService;
    private readonly ILogger<ErasResolver> _logger;

    public ErasResolver(
        IGetActiveErasConnection getActiveErasConnection, 
        IEraService eraService,
        ILogger<ErasResolver> logger
    )
    {
        _eraService = eraService;
        _getActiveErasConnection = getActiveErasConnection;
        _logger = logger;
    }
    
    public async Task Resolve()
    {
        const int maxRetries = 10;
        const int retryDelayMs = 30000; // 30 seconds
        int retryCount = 0;

        while (true)
        {
            try
            {
                var latestEra = await _eraService.GetLatestFinishedEra();
                Console.WriteLine(latestEra);
                _logger.LogInformation("Fetching active eras from era index {EraIndex}", latestEra?.EraIndex ?? -1);
                var eraTypes = await _getActiveErasConnection.FetchActiveEras(latestEra?.EraIndex ?? -1);
                _logger.LogInformation("Fetched {EraTypesCount} active eras", eraTypes.Count);
                await _eraService.ResolveActiveErasAndSave(eraTypes);
                // No era found, we need to insert genesis era
                if (latestEra == null || latestEra?.EraIndex <= 0)
                {
                    await InsertGenesisEra();
                }
                break; // Success - exit the retry loop
            }
            catch (Exception e)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    _logger.LogError(e, $"Error fetching active eras after {retryCount} retries. Giving up.");
                    throw new Exception("Error fetching active eras", e);
                }
                _logger.LogWarning(e, $"Error fetching active eras. Retry {retryCount}/{maxRetries} in {retryDelayMs}ms");
                await Task.Delay(retryDelayMs);
            }
        }
    }

    // There is a chance that the start block of the first era is not 0, in this case, we need to insert another era
    // with index -1 from block 0 to the start of the first era
    private async Task InsertGenesisEra()
    {
        var firstEra = await _eraService.GetEraByIndex(0);
        if (firstEra.StartBlock == 0) return; // No need to insert genesis era
        _logger.LogInformation("Inserting Genesis Era for eraIndex -1. StartBlock: 0, EndBlock: {StartBlock}", firstEra.StartBlock - 1);
        var genesisEra = new EraModel
        {
            EraIndex = -1,
            StartBlock = 0,
            EndBlock = firstEra.StartBlock - 1
        };
        await _eraService.InsertEra(genesisEra);
    }
}