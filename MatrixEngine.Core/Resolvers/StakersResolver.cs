using MatrixEngine.Core.GraphQL.Stakers;
using MatrixEngine.Core.Services;

namespace MatrixEngine.Core.Resolvers;

public interface IStakersResolver
{
    Task FetchStakersByEraIndex(int eraIndex);
    Task Resolve();
}

public class StakersResolver : IStakersResolver
{
    private readonly IGetStakersConnection _getStakersConnection;
    private readonly IStakerService _stakerService;
    private readonly IEraService _eraService;

    public StakersResolver(IGetStakersConnection getStakersConnection, IStakerService stakerService, IEraService eraService)
    {
        _eraService = eraService;
        _stakerService = stakerService;
        _getStakersConnection = getStakersConnection;
    }
    
    public async Task FetchStakersByEraIndex(int eraIndex)
    {
        var stakerTypes = await _getStakersConnection.FetchStakers(eraIndex);
        await _stakerService.ResolveStakersAndSave(stakerTypes);
    }

    public async Task Resolve()
    {
        var latestEra = await _eraService.GetLatestFinishedEra();
        var latestFetchStakerEraIndex = await _stakerService.GetLatestEraFetchedStakerTypes();
        //loop eras that have not fetched stakers
        for(var i = latestFetchStakerEraIndex + 1; i <= latestEra.EraIndex; i++)
        {
            await FetchStakersByEraIndex(i);
        }
    }
}