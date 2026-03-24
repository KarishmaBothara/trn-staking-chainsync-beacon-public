using System.Diagnostics;
using MatrixEngine.Core.Substrate.Ledger;
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
    private readonly ISubstrateLedgerClient _substrateLedgerClient;
    private readonly ILogger<EngineCoreCore> _logger;

    public EngineCoreCore(
        IDataCore dataCore,
        IComputingCore computingCore,
        ISubstrateLedgerClient substrateLedgerClient,
        ILogger<EngineCoreCore> logger)
    {
        _logger = logger;
        _computingCore = computingCore;
        _substrateLedgerClient = substrateLedgerClient;
        _dataCore = dataCore;
    }

    public async Task Start()
    {
        _logger.LogTrace("EngineCore started.");
        await _dataCore.ResolveDataFromIndexer();
        _logger.LogTrace("Data Resolved.");
        await _computingCore.Compute();
    }
}