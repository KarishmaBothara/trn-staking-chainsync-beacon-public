using MatrixEngine.Core.Engine;
using MatrixEngine.Core.GraphQL.ActiveEras;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Testing.Fixtures;
using Moq;

namespace MatrixEngine.Core.Testing.Resolvers;

public class ErasResolverTests
{
    private readonly Mock<IGetActiveErasConnection> _getActiveErasConnection;
    private readonly Mock<IEraService> _eraService;

    public ErasResolverTests()
    {
        _getActiveErasConnection = new Mock<IGetActiveErasConnection>();
        _eraService = new Mock<IEraService>();
    }

    [Fact]
    public async Task Resolve_WhenApiReturns()
    {
        var mockEraTypes = GraphQLFixture.LoadEras(1);
        var activeEras = mockEraTypes.Data.ActiveErasConnection.Edges.Select(m => m.Node).ToList();
        _getActiveErasConnection.Setup(m => m.FetchActiveEras()).ReturnsAsync(activeEras);

        var erasResolver = new ErasResolver(_getActiveErasConnection.Object, _eraService.Object);
        await erasResolver.Resolve();

        _getActiveErasConnection.Verify(m => m.FetchActiveEras(), Times.Once);
        _eraService.Verify(m => m.ResolveActiveErasAndSave(It.Is<List<ActiveEraType>>(a => a == activeEras)));
    }
}