using MatrixEngine.Core.Engine;
using MatrixEngine.Core.Resolvers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatrixEngine.Core.Testing.Engine;

public class DataCoreTests
{
    private readonly Mock<IErasResolver> _eraResolver;
    private readonly Mock<IStakersResolver> _stakersResolver;
    private readonly Mock<ITransactionEventsResolver> _transactionEventsResolver;
    private readonly Mock<ILogger<DataCore>> _logger;

    public DataCoreTests()
    {
        _eraResolver = new Mock<IErasResolver>();
        _stakersResolver = new Mock<IStakersResolver>();
        _transactionEventsResolver = new Mock<ITransactionEventsResolver>();
        _logger = new Mock<ILogger<DataCore>>();
    }
    
    [Fact]
    public async Task ResolveDataFromIndexer_WhenCalled_ShouldCallAllResolvers()
    {
        var dataCore = new DataCore(_eraResolver.Object, _stakersResolver.Object, _transactionEventsResolver.Object, _logger.Object);
        
        await dataCore.ResolveDataFromIndexer();
        
        _eraResolver.Verify(m => m.Resolve(), Times.Once);
        _stakersResolver.Verify(m => m.Resolve(), Times.Once);
        _transactionEventsResolver.Verify(m => m.Resolve(), Times.Once);
    }
}