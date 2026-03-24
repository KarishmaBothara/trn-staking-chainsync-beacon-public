using System.Numerics;
using MatrixEngine.Core.Resolvers;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.Engine;

public interface IDataCore
{
    Task ResolveDataFromIndexer();
}

public class DataCore : IDataCore
{
    private readonly IErasResolver _erasResolver;
    private readonly IStakersResolver _stakersResolver;
    private readonly ITransactionEventsResolver _transactionEventsResolver;
    private readonly IChilledResolver _chilledResolver;
    private readonly ILogger<DataCore> _logger;

    public DataCore(
        IErasResolver erasResolver, 
        IStakersResolver stakersResolver,
        ITransactionEventsResolver transactionEventsResolver,
        IChilledResolver chilledResolver,
        ILogger<DataCore> logger)
    {
        _logger = logger;
        _transactionEventsResolver = transactionEventsResolver;
        _stakersResolver = stakersResolver;
        _erasResolver = erasResolver;
        _chilledResolver = chilledResolver;
    }
    
    
    public async Task ResolveDataFromIndexer()
    {
        _logger.LogTrace("Resolving Data from Indexer.");
        _logger.LogTrace("Resolving Eras.");
        await _erasResolver.Resolve();
        _logger.LogTrace("Resolving stakers.");
        await _stakersResolver.Resolve();
        _logger.LogTrace("Resolving transaction events.");
        await _transactionEventsResolver.Resolve();
        _logger.LogTrace("Resolving chilled events.");
        await _chilledResolver.Resolve();
    }
    
}