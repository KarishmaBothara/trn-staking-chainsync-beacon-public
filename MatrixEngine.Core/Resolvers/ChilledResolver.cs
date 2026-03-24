using MatrixEngine.Core.GraphQL.Chilled;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Services;
using MongoDB.Bson;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.Resolvers;

public interface IChilledResolver
{
    Task Resolve();
}

public class ChilledResolver : IChilledResolver
{
    private readonly IGetChilledConnection _getChilledConnection;
    private readonly IChilledService _chilledService;
    private readonly ILogger<ChilledResolver> _logger;

    public ChilledResolver(IGetChilledConnection getChilledConnection, IChilledService chilledService, ILogger<ChilledResolver> logger)
    {
        _chilledService = chilledService;
        _getChilledConnection = getChilledConnection;
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
                var chilledEvents = await _getChilledConnection.FetchChilledEvents();
                var chilledModels = chilledEvents.Select(e => new ChilledModel
                {
                    Id = ObjectId.GenerateNewId(),
                    Account = e.Stash,
                    BlockNumber = e.BlockNumber
                }).ToList();
                
                await _chilledService.UpsertChilledEvents(chilledModels);
                break; // Success - exit the retry loop
            }
            catch (Exception e)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    _logger.LogError(e, $"Error fetching chilled events after {retryCount} retries. Giving up.");
                    throw new Exception("Error fetching chilled events", e);
                }
                _logger.LogWarning(e, $"Error fetching chilled events. Retry {retryCount}/{maxRetries} in {retryDelayMs}ms");
                await Task.Delay(retryDelayMs);
            }
        }
    }
} 