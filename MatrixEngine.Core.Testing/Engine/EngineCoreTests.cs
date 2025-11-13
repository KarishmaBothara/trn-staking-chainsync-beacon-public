using MatrixEngine.Core.Engine;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatrixEngine.Core.Testing.Engine;

public class EngineCoreCoreTests
{
    private readonly Mock<IDataCore> _datacore;
    private readonly Mock<IComputingCore> _computingCore;
    private readonly Mock<ILogger<Core.Engine.EngineCoreCore>> _logger;
    private readonly Core.Engine.EngineCoreCore _engineCoreCore;

    public EngineCoreCoreTests()
    {
        _datacore = new Mock<IDataCore>();
        _computingCore = new Mock<IComputingCore>();
        _logger = new Mock<ILogger<Core.Engine.EngineCoreCore>>();
        
        _engineCoreCore = new Core.Engine.EngineCoreCore(_datacore.Object, _computingCore.Object, _logger.Object);
    }
    
    [Fact]
    public async Task Start_WhenCalled_ShouldCallDataCoreAndComputingCore()
    {
        await _engineCoreCore.Start();
        
        _datacore.Verify(m => m.ResolveDataFromIndexer(), Times.Once);
        _computingCore.Verify(m => m.Compute(), Times.Once);
    }
}