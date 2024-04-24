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
    private readonly ILogger<DataCore> _logger;

    public DataCore(
        IErasResolver erasResolver, IStakersResolver stakersResolver,
        ITransactionEventsResolver transactionEventsResolver,
        ILogger<DataCore> logger)
    {
        _logger = logger;
        _transactionEventsResolver = transactionEventsResolver;
        _stakersResolver = stakersResolver;
        _erasResolver = erasResolver;
    }
    
    
    public async Task ResolveDataFromIndexer()
    {
        _logger.LogTrace("Resolving Data from Indexer.");
        await _erasResolver.Resolve();
        await _stakersResolver.Resolve();
        await _transactionEventsResolver.Resolve();
    }
}