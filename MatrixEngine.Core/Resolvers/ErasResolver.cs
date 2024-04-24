using MatrixEngine.Core.GraphQL.ActiveEras;
using MatrixEngine.Core.Services;

namespace MatrixEngine.Core.Resolvers;

public interface IErasResolver
{
    Task Resolve();
}

public class ErasResolver : IErasResolver
{
    private IGetActiveErasConnection _getActiveErasConnection;
    private IEraService _eraService;

    public ErasResolver(IGetActiveErasConnection getActiveErasConnection, IEraService eraService)
    {
        _eraService = eraService;
        _getActiveErasConnection = getActiveErasConnection;
    }
    
    public async Task Resolve()
    {
        var eraTypes = await _getActiveErasConnection.FetchActiveEras();
        await _eraService.ResolveActiveErasAndSave(eraTypes);
    }
}