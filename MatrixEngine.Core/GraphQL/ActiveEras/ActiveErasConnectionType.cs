using MatrixEngine.Core.GraphQL.Common;

namespace MatrixEngine.Core.GraphQL.ActiveEras;


public class GetActiveErasConnectionResponseType
{
    public ActiveErasConnectionType ActiveErasConnection { get; set; }
} 


public class ActiveErasConnectionType
{
    public int TotalCount { get; set; }
    
    public PageInfoType PageInfo { get; set; }
    

    public List<EdgeType<ActiveEraType>> Edges { get; set; }
}

public class ActiveEraType
{
    public int EraIndex { get; set; }
    public int BlockNumber { get; set; }
    public DateTime Timestamp { get; set; }
}