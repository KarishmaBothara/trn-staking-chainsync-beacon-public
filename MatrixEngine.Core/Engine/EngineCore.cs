using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.Engine;

public interface IEngineCore
{
    Task Start();
}

public class EngineCoreCore : IEngineCore
{
    private readonly IDataCore _dataCore;
    private readonly IComputingCore _computingCore;
    private readonly ILogger<EngineCoreCore> _logger;

    public EngineCoreCore(
        IDataCore dataCore,
        IComputingCore computingCore,
        ILogger<EngineCoreCore> logger)
    {
        _logger = logger;
        _computingCore = computingCore;
        _dataCore = dataCore;
    }

    public async Task Start()
    {
        _logger.LogTrace("EngineCore started.");
        
        await _dataCore.ResolveDataFromIndexer();
        await _computingCore.Compute();
        await _dataCore.ValidateData();
    }
}