using MatrixEngine.Core.Engine;
using MatrixEngine.Core.GraphQL.Stakers;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Testing.Fixtures;
using Moq;

namespace MatrixEngine.Core.Testing.Resolvers;

public class StakersResolverTests
{
    private readonly Mock<IGetStakersConnection> _getStakersConnection;
    private readonly Mock<IStakerService> _stakerService;
    private readonly Mock<IEraService> _eraService;
    private readonly StakersResolver _stakersResolver;

    public StakersResolverTests()
    {
        _getStakersConnection = new Mock<IGetStakersConnection>();
        _stakerService = new Mock<IStakerService>();
        _eraService = new Mock<IEraService>();
        _stakersResolver = new StakersResolver(_getStakersConnection.Object, _stakerService.Object, _eraService.Object);
    }

    [Fact]
    public async Task Resolve_WhenApiReturns()
    {
        var mockStakerTypes = GraphQLFixture.LoadStakers(1);
        var stakerTypes = mockStakerTypes.Data.StakersConnection.Edges.Select(m => m.Node).ToList();
        _getStakersConnection.Setup(m => m.FetchStakers(1)).ReturnsAsync(stakerTypes);

        await _stakersResolver.FetchStakersByEraIndex(1);

        _getStakersConnection.Verify(m => m.FetchStakers(1), Times.Once);
        _stakerService.Verify(m => m.ResolveStakersAndSave(It.Is<List<StakerNodeType>>(s => s == stakerTypes)));
    }
    
    [Fact]
    public async Task FetchStakersUpToLatestEra_WhenLatestEraReturns_AndShouldCallThreeTimes()
    {
        var latestEra = new EraModel
        {
            EraIndex = 3 
        };
        const int latestFetchStakerEraIndex = 0;
        _eraService.Setup(m => m.GetLatestFinishedEra()).ReturnsAsync(latestEra);
        _stakerService.Setup(m => m.GetLatestEraFetchedStakerTypes()).ReturnsAsync(latestFetchStakerEraIndex);
        _getStakersConnection.Setup(m => m.FetchStakers(1)).ReturnsAsync(new List<StakerNodeType>());

        await _stakersResolver.Resolve();

        _eraService.Verify(m => m.GetLatestFinishedEra(), Times.Once);
        _stakerService.Verify(m => m.GetLatestEraFetchedStakerTypes(), Times.Once);
        _getStakersConnection.Verify(m => m.FetchStakers(It.IsAny<int>()), Times.Exactly(3));
        _stakerService.Verify(m => m.ResolveStakersAndSave(It.IsAny<List<StakerNodeType>>()), Times.Exactly(3));
    }
}